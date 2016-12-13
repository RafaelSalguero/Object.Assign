using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ObjectAssign.Visitors;

namespace Tonic
{

    /// <summary>
    /// Exposes the assign method that create an expression that copies all properties from one object onto another
    /// </summary>
    public static partial class LinqEx
    {
        /// <summary>
        /// A pair of properties that are mapped from source to dest
        /// </summary>
        public class PropertyMapping
        {
            /// <summary>
            /// Destination property
            /// </summary>
            public PropertyInfo Dest { get; set; }
            /// <summary>
            /// Source property
            /// </summary>
            public PropertyInfo Source { get; set; }
        }

        /// <summary>
        /// MapTypesSlow result cache
        /// </summary>
        [ThreadStatic]
        static Dictionary<Tuple<Type, Type>, Dictionary<string, PropertyMapping>> mappingsCache;

        /// <summary>
        /// Map without using the cache
        /// Only map a pair of properties if the source property can be readed and if the dest property can be written
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<string, PropertyMapping> MapTypesSlow(IEnumerable<PropertyInfo> SourceProperties, IEnumerable<PropertyInfo> DestProperties)
        {

            var SourceProps = SourceProperties.ToDictionary(x => x.Name);
            return
                DestProperties
                .Where(x => SourceProps.ContainsKey(x.Name))                        //Filter only properties that are both on source and dest
                .Select(x => new { DestProp = x, SourceProp = SourceProps[x.Name] })//Map by name between
                .Where(x => x.DestProp.PropertyType.IsAssignableFrom(x.SourceProp.PropertyType))   //Filter out properties with the same name but different type or types that are not assignable
                .Where(x => x.SourceProp.CanRead && x.DestProp.CanWrite) //Only pass when the source can be readed and when DestProp can be written
                .Select(x => new PropertyMapping { Dest = x.DestProp, Source = x.SourceProp })
                .ToDictionary(x => x.Dest.Name);
        }

        /// <summary>
        /// Map all properties that are both in source and dest.
        /// The property comparission takes the property name and property type.
        /// This method is used internally by the Assign method.
        /// Only map a pair of properties if the source property can be readed and if the dest property can be written
        /// </summary>
        /// <param name="Source">Source type</param>
        /// <param name="Dest">Dest type</param>
        /// <returns></returns>
        internal static Dictionary<string, PropertyMapping> MapTypes(Type Source, Type Dest)
        {
            var Key = Tuple.Create(Source, Dest);
            Dictionary<string, PropertyMapping> Result;
            if (mappingsCache == null)
                mappingsCache = new Dictionary<Tuple<Type, Type>, Dictionary<string, Tonic.LinqEx.PropertyMapping>>();

            if (!mappingsCache.TryGetValue(Key, out Result))
            {
                Result = MapTypesSlow(Source.GetProperties(), Dest.GetProperties());
                mappingsCache.Add(Key, Result);
            }
            return Result;
        }

        /// <summary>
        /// Extract a dictionary from bindings from a member initialization expression
        /// </summary>
        /// <typeparam name="TIn">Source type</typeparam>
        /// <typeparam name="TOut">Dest type</typeparam>
        /// <param name="expression">Member initialization expression</param>
        /// <param name="inputParameter">Expression that will replace the input parameter of the given expression or null to not replace the input parameter</param>
        /// <returns></returns>
        internal static Dictionary<string, MemberAssignment> ExtractBindings<TIn, TOut>(Expression<Func<TIn, TOut>> expression, Expression inputParameter = null)
        {
            if (inputParameter == null)
            {
                inputParameter = expression.Parameters[0];
            }

            if (!(expression.Body is MemberInitExpression))
                throw new ArgumentException("The body of the expression isn't a member initialization expression");

            var MI = (MemberInitExpression)expression.Body;

            var result = new Dictionary<string, MemberAssignment>();
            var replace = new ReplaceVisitor(expression.Parameters[0], inputParameter);

            foreach (var Binding in MI.Bindings)
            {
                if (!(Binding is MemberAssignment))
                    throw new ArgumentException("Only bindings of member assignment are allowed");

                var BindingAux = (MemberAssignment)Binding;
                var BindingExpr = replace.Visit(BindingAux.Expression);

                result.Add(BindingAux.Member.Name, Expression.Bind(BindingAux.Member, BindingExpr));
            }

            return result;
        }




        /// <summary>
        /// Clone properties from the source object onto the dest object mapping properties by type and name.
        /// </summary>
        /// <param name="Dest">The object that will be populated</param>
        /// <param name="Source">The object to read properties from</param>
        public static void PopulateObject(object Source, object Dest)
        {
            PopulateObject(Source, Dest, x => true);
        }

        private static bool SimplePropertyMappingPredicate(PropertyMapping x)
        {
            return x.Source.PropertyType.GetCustomAttribute<ComplexTypeAttribute>() != null ||
            IsSimpleType(x.Dest.PropertyType);
        }

        /// <summary>
        /// Clone properties from the source object onto the dest object mapping properties by type and name. Only clone properties 
        /// with simple types. All values types, primitive types and the string type are considered simple.
        /// Types marked with the ComplexType attribute are also populated or created if null is found on dest properties
        /// </summary>
        /// <param name="Dest">The object that will be populated</param>
        /// <param name="Source">The object to read properties from</param>
        public static void PopulateObjectSimple(object Source, object Dest)
        {
            PopulateObject(Source, Dest, SimplePropertyMappingPredicate, true);
        }

        /// <summary>
        /// Set the properties of an object that are present on the given memeber initialization expression
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="instance"></param>
        /// <param name="memberInitialization"></param>
        public static void PopulateObject<T, TOut>(T source, TOut dest, Expression<Func<T, TOut>> memberInitialization)
        {
            ParameterExpression param = Expression.Parameter(typeof(T));
            ParameterExpression destParam = Expression.Parameter(typeof(TOut));
            var initExpressions = ExtractBindings(memberInitialization, param);
            Action<T, TOut> result;

            if (initExpressions.Count == 0)
            {
                result = (T a, TOut b) => { };
            }
            else
            {
                var block = new List<Expression>();
                foreach (var m in initExpressions.Values)
                {
                    var member = m.Member;
                    var expr = Expression.Assign(Expression.PropertyOrField(destParam, member.Name), m.Expression);
                    block.Add(expr);
                }

                var lambda = Expression.Lambda<Action<T, TOut>>(Expression.Block(block), param, destParam);
                result = lambda.Compile();
            }

            result(source, dest);
        }

        /// <summary>
        /// Clone properties from the source object onto the dest object mapping properties by type and name. Cloned properties can be further filtered with the
        /// property mapping predicate
        /// </summary>
        /// <param name="Dest">The object that will be populated</param>
        /// <param name="Source">The object to read properties from</param>
        /// <param name="PropertyMappingPredicate">After the properties where matched by type and name, a filter with this predicate is applied to the property mappings.</param>
        /// <param name="deepClone">True to deep clone the object</param>
        public static void PopulateObject(object Source,
            object Dest,
            Func<PropertyMapping, bool> PropertyMappingPredicate,
            bool deepClone = false)
        {
            var DestType = Dest.GetType();
            var Props = MapTypes(Source.GetType(), DestType);

            var Binds = Props
                .Where(x => PropertyMappingPredicate(x.Value))
                .Select(x =>
                {
                //Select Dest property and property values from property mappings
                object value;
                    var sourceValue = x.Value.Source.GetValue(Source);
                    if (sourceValue != null && !IsSimpleType(sourceValue.GetType()) && deepClone)
                    {
                    //deep clone:
                    value = x.Value.Dest.CanRead ? x.Value.Dest.GetValue(Dest) : null;

                    //Create an instance if dest is null:
                    if (value == null)
                            value = Activator.CreateInstance(x.Value.Dest.PropertyType);
                        PopulateObject(sourceValue, value, PropertyMappingPredicate, true);
                    }
                    else
                    {
                        value = sourceValue;
                    }

                    return new
                    {
                        Dest = x.Value.Dest,
                        Value = value
                    };
                })
                .ToArray();

            //Execute the bindings:
            foreach (var b in Binds)
                b.Dest.SetValue(Dest, b.Value);
        }

        /// <summary>
        /// Create the member assignments that initialize an object of type TOut with all properties of type TIn using the member initizer sintax
        /// The user can override or add new member bindings
        /// </summary>
        /// <param name="TIn">The source type</param>
        /// <param name="TOut">The object of the type that will be member initialized</param>
        /// <param name="otherBindings">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <param name="PropertyMappingPredicate">After the properties where matched by type and name, a filter with this predicate is applied to the property mappings</param>
        /// <param name="deepClone">True to generate a deep clone expression, false for shallow clone</param>
        /// <param name="inputParameter">input parameter of type TIn</param>
        /// <returns></returns>
        public static MemberAssignment[] CloneExpression(
            Type TIn,
            Type TOut,
            Expression inputParameter,
            Dictionary<string, MemberAssignment> otherBindings,
            Func<PropertyMapping, bool> PropertyMappingPredicate,
            bool deepClone)
        {
            var Props = MapTypes(TIn, TOut).Where(x => PropertyMappingPredicate(x.Value));

            var Binds = Props
                .Where(x => !otherBindings.ContainsKey(x.Key))  //Ignore explicit bindings on other members
                .Select(P =>
                {
                    var sourceProp = Expression.Property(inputParameter, P.Value.Source);
                    Expression destValue;
                //Deep cloning:
                if (!IsSimpleType(P.Value.Source.PropertyType) && deepClone)
                    {
                        var assignments = CloneExpression(P.Value.Source.PropertyType, P.Value.Source.PropertyType, sourceProp, new Dictionary<string, MemberAssignment>(), PropertyMappingPredicate, true);
                        destValue = Expression.MemberInit(Expression.New(P.Value.Dest.PropertyType), assignments);
                    }
                    else
                    {
                        destValue = sourceProp;
                    }

                    return Expression.Bind(P.Value.Dest, destValue);
                })
                .Concat(otherBindings.Values)   //Add member asignments
                .ToArray();

            return Binds;
        }

        /// <summary>
        /// Create an expression that initialize an object of type TOut with all properties of type TIn using the member initizer sintax
        /// The user can override or add new member bindings
        /// </summary>
        /// <typeparam name="TIn">The source type</typeparam>
        /// <typeparam name="TOut">The object of the type that will be member initialized</typeparam>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <param name="PropertyMappingPredicate">After the properties where matched by type and name, a filter with this predicate is applied to the property mappings</param>
        /// <param name="deepClone">True to generate a deep clone expression, false for shallow clone</param>
        /// <returns></returns>
        public static Expression<Func<TIn, TOut>> Clone<TIn, TOut>(
            Expression<Func<TIn, TOut>> otherMembers,
            Func<PropertyMapping, bool> PropertyMappingPredicate,
            bool deepClone = false)
        {
            var param = Expression.Parameter(typeof(TIn), "x");
            var otherBindings = otherMembers == null ? new Dictionary<string, MemberAssignment>() : ExtractBindings(otherMembers, param);

            var binds = CloneExpression(typeof(TIn), typeof(TOut), param, otherBindings, PropertyMappingPredicate, deepClone);
            var body = Expression.MemberInit(Expression.New(typeof(TOut)), binds);

            return Expression.Lambda<Func<TIn, TOut>>(body, param);
        }

        /// <summary>
        /// Create an expression that initialize an object of type TOut with all properties of type TIn using the member initizer sintax
        /// The user can override or add new member bindings.
        /// This is a shallow clone
        /// </summary>
        /// <typeparam name="TIn">The source type</typeparam>
        /// <typeparam name="TOut">The object of the type that will be member initialized</typeparam>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <returns></returns>
        public static Expression<Func<TIn, TOut>> Clone<TIn, TOut>(Expression<Func<TIn, TOut>> otherMembers = null)
        {
            return Clone(otherMembers, x => true, false);
        }

        /// <summary>
        /// Returns an expression that clone all simple types properties and
        /// deep clone properties with types with the ComplexType attribute
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <returns></returns>
        public static Expression<Func<TIn, TOut>> CloneSimple<TIn, TOut>(Expression<Func<TIn, TOut>> otherMembers = null)
        {
            return Clone(otherMembers, SimplePropertyMappingPredicate, true);
        }

        /// <summary>
        /// Call the Select method from the given queryable with the clone expression
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="query">The query to proyect</param>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <returns></returns>
        public static IQueryable<TOut> SelectClone<T, TOut>(this IQueryable<T> query, Expression<Func<T, TOut>> otherMembers = null)
        {
            return query.Select(Clone(otherMembers));
        }

        /// <summary>
        /// Clones all properties that have the same name and type from input type to a new instance of output type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="query">The query to proyect</param>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <returns></returns>
        public static IEnumerable<TOut> SelectClone<T, TOut>(this IEnumerable<T> query, Expression<Func<T, TOut>> otherMembers = null)
            where TOut : new()
        {
            return query.Select(x =>
            {
                var ret = new TOut();
                PopulateObject(x, ret);
                if (otherMembers != null)
                    PopulateObject(x, ret, otherMembers);
                return ret;
            });
        }

        /// <summary>
        /// Clones all properties that have the same name and type from input type to a new instance of output type.
        /// Only properties that have 'simple' types are copied by default. All value-types, primitive types, and the string type are considered simple
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="query">The query to proyect</param>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <returns></returns>
        public static IEnumerable<TOut> SelectCloneSimple<T, TOut>(this IEnumerable<T> query, Expression<Func<T, TOut>> otherMembers = null)
            where TOut : new()
        {
            return query.Select(x =>
            {
                var ret = new TOut();
                PopulateObjectSimple(x, ret);
                if (otherMembers != null)
                    PopulateObject(x, ret, otherMembers);
                return ret;
            });
        }

        /// <summary>
        /// Returns true if the type is a value type, a primitive, or the type String
        /// </summary>
        /// <param name="type">The type to check</param>
        public static bool IsSimpleType(this Type type)
        {
            return
                type.IsValueType ||
                type.IsPrimitive ||
                type == typeof(string);
        }

        /// <summary>
        /// Clones all properties that have the same name and type. 
        /// Only properties that have 'simple' types or types with the ComplexType attribute are copied by default. 
        /// All value-types, primitive types, and the string type are considered simple.
        /// Complex properties marked with the ComplexType attribute are deep cloned
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="query">The query to proyect</param>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <returns></returns>
        public static IQueryable<TOut> SelectCloneSimple<T, TOut>(this IQueryable<T> query, Expression<Func<T, TOut>> otherMembers = null)
            where TOut : new()
        {
            return query.Select(CloneSimple(otherMembers));
        }

        /// <summary>
        /// Clones all properties that have the same name and type from input type to a new instance of output type.
        /// This results in a shallow clone
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="query">The query to proyect</param>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <param name="PropertyMappingPredicate">After the properties where matched by type and name, a filter with this predicate is applied to the property mappings</param>
        /// <returns></returns>
        public static IQueryable<TOut> SelectClone<T, TOut>(this IQueryable<T> query, Expression<Func<T, TOut>> otherMembers, Func<PropertyMapping, bool> PropertyMappingPredicate)
            where TOut : new()
        {
            return query.Select(Clone(otherMembers, PropertyMappingPredicate, false));
        }


    }

}
