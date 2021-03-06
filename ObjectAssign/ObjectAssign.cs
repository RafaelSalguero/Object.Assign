﻿using System;
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
            /// Available source properties
            /// </summary>
            public PropertyInfo Source { get; set; }
        }

        /// <summary>
        /// MapTypesSlow result cache
        /// </summary>
        [ThreadStatic]
        static Dictionary<Tuple<Type, Type>, IEnumerable<PropertyMapping>> mappingsCache;

        /// <summary>
        /// Map without using the cache
        /// Only map a pair of properties if the source property can be readed and if the dest property can be written
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<PropertyMapping> MapTypesSlow(IEnumerable<PropertyInfo> SourceProperties, IEnumerable<PropertyInfo> DestProperties)
        {
            //Recordamos que puede haber varias propiedades con el mismo nombre, en caso de una herencia, al esconder una propiedad con otra propiedad de un tipo diferente:
            var SourceProps = SourceProperties
                .Where(x => x.CanRead)
                .GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToList());

            return
                DestProperties
                .Where(x => x.CanWrite) //Only writable dest properties
                .Where(x => SourceProps.ContainsKey(x.Name))                        //Filter only properties that are both on source and dest
                .Select(x => new
                {
                    DestProp = x,
                    SourceProps = SourceProps[x.Name].Where(src => x.PropertyType.IsAssignableFrom(src.PropertyType))// filter type-compatible properties
                })
                .Where(x => x.SourceProps.Any()) //Filter out any pair without any compatible properties
                .Select(x =>
                new PropertyMapping
                {
                    Dest = x.DestProp,
                    Source = x.SourceProps.First()
                });
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
        internal static IEnumerable<PropertyMapping> MapTypes(Type Source, Type Dest)
        {
            var Key = Tuple.Create(Source, Dest);
            IEnumerable<PropertyMapping> Result;
            if (mappingsCache == null)
                mappingsCache = new Dictionary<Tuple<Type, Type>, IEnumerable<PropertyMapping>>();

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
        /// <param name="expression">Member initialization expression</param>
        /// <returns></returns>
        internal static Dictionary<string, MemberAssignment> ExtractBindings(LambdaExpression expression)
        {
            if (!(expression.Body is MemberInitExpression))
                throw new ArgumentException("The body of the expression isn't a member initialization expression");

            var MI = (MemberInitExpression)expression.Body;

            var result = new Dictionary<string, MemberAssignment>();

            foreach (var Binding in MI.Bindings)
            {
                if (!(Binding is MemberAssignment))
                    throw new ArgumentException("Only bindings of member assignment are allowed");

                var BindingAux = (MemberAssignment)Binding;
                var BindingExpr = BindingAux.Expression;

                result.Add(BindingAux.Member.Name, Expression.Bind(BindingAux.Member, BindingExpr));
            }

            return result;
        }

        /// <summary>
        /// Combine two dictionaries, b dictionary elements takes precedence over a elements, if the key is present on both dictionaries
        /// </summary>
        /// <param name="a">First dictionary</param>
        /// <param name="b">Second dictionary. This dictionary takes precedence over the second</param>
        /// <returns></returns>
        internal static Dictionary<TKey, TValue> CombineDictionary<TKey, TValue>(Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b)
        {
            var ret = new Dictionary<TKey, TValue>(a);
            foreach (var k in b)
                ret[k.Key] = k.Value;

            return ret;
        }

        /// <summary>
        /// Replace input with output parameters
        /// </summary>
        static T ReplaceParameters<T>(T expression, IReadOnlyList<ParameterExpression> inputParameters, IReadOnlyList<ParameterExpression> outputParameters)
            where T : Expression
        {
            if (inputParameters.Count != outputParameters.Count)
            {
                throw new ArgumentException(nameof(inputParameters));
            }
            var ret = expression;
            for (var i = 0; i < inputParameters.Count; i++)
            {
                ret = (T)new ReplaceVisitor(inputParameters[i], outputParameters[i]).Visit(ret);
            }
            return ret;
        }

        static Expression<Func<T1, TR>> TypeLambda<T1, TR>(LambdaExpression lambda) => Expression.Lambda<Func<T1, TR>>(lambda.Body, lambda.Parameters);
        static Expression<Func<T1, T2, TR>> TypeLambda<T1, T2, TR>(LambdaExpression lambda) => Expression.Lambda<Func<T1, T2, TR>>(lambda.Body, lambda.Parameters);
        static Expression<Func<T1, T2, T3, TR>> TypeLambda<T1, T2, T3, TR>(LambdaExpression lambda) => Expression.Lambda<Func<T1, T2, T3, TR>>(lambda.Body, lambda.Parameters);
        static Expression<Func<T1, T2, T3, T4, TR>> TypeLambda<T1, T2, T3, T4, TR>(LambdaExpression lambda) => Expression.Lambda<Func<T1, T2, T3, T4, TR>>(lambda.Body, lambda.Parameters);

        public static Expression<Func<T1, TR>> CombineMemberInitExpression<T1, TR>(Expression<Func<T1, TR>> a, Expression<Func<T1, TR>> b) => TypeLambda<T1, TR>(CombineMemberInitExpression((LambdaExpression)a, b));
        public static Expression<Func<T1, T2, TR>> CombineMemberInitExpression<T1, T2, TR>(Expression<Func<T1, T2, TR>> a, Expression<Func<T1, T2, TR>> b) => TypeLambda<T1, T2, TR>(CombineMemberInitExpression((LambdaExpression)a, b));
        public static Expression<Func<T1, T2, T3, TR>> CombineMemberInitExpression<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> a, Expression<Func<T1, T2, T3, TR>> b) => TypeLambda<T1, T2, T3, TR>(CombineMemberInitExpression((LambdaExpression)a, b));
        public static Expression<Func<T1, T2, T3, T4, TR>> CombineMemberInitExpression<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> a, Expression<Func<T1, T2, T3, T4, TR>> b) => TypeLambda<T1, T2, T3, T4, TR>(CombineMemberInitExpression((LambdaExpression)a, b));


        /// <summary>
        /// Combie two member initialization expressions. The second expression takes precedence over the first one if the same property initializer is present on both expressions
        /// </summary>
        /// <returns></returns>
        public static LambdaExpression CombineMemberInitExpression(LambdaExpression a, LambdaExpression b)
        {
            if (!a.Parameters.Select(x => x.Type).SequenceEqual(b.Parameters.Select(x => x.Type)))
            {
                throw new ArgumentException("Expressions input parameters must match");
            }
            if (a.ReturnType != b.ReturnType)
            {
                throw new ArgumentException("Expressions return types must match ");
            }

            var outputParameters = a.Parameters.Select(x => Expression.Parameter(x.Type, x.Name + "_2")).ToList();
            var aBindings = ExtractBindings(ReplaceParameters(a, a.Parameters, outputParameters));
            var bBindings = ExtractBindings(ReplaceParameters(b, b.Parameters, outputParameters));


            var combine = CombineDictionary(aBindings, bBindings);

            var expr = Expression.MemberInit((b.Body as MemberInitExpression).NewExpression, combine.Values);

            return Expression.Lambda(expr, outputParameters);
        }

        public static Expression<Func<T1, TOut>> CombineMemberInitExpression<T1, TOut>(params Expression<Func<T1, TOut>>[] expressions) => expressions.Aggregate((a, b) => CombineMemberInitExpression(a, b));
        public static Expression<Func<T1, T2, TOut>> CombineMemberInitExpression<T1, T2, TOut>(params Expression<Func<T1, T2, TOut>>[] expressions) => expressions.Aggregate((a, b) => CombineMemberInitExpression(a, b));
        public static Expression<Func<T1, T2, T3, TOut>> CombineMemberInitExpression<T1, T2, T3, TOut>(params Expression<Func<T1, T2, T3, TOut>>[] expressions) => expressions.Aggregate((a, b) => CombineMemberInitExpression(a, b));
        public static Expression<Func<T1, T2, T3, T4, TOut>> CombineMemberInitExpression<T1, T2, T3, T4, TOut>(params Expression<Func<T1, T2, T3, T4, TOut>>[] expressions) => expressions.Aggregate((a, b) => CombineMemberInitExpression(a, b));


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
        public static void CopyMembers<T, TOut>(T source, TOut dest, Expression<Func<T, TOut>> memberInitialization)
        {
            ParameterExpression param = Expression.Parameter(typeof(T));
            ParameterExpression destParam = Expression.Parameter(typeof(TOut));
            var initExpressions = ExtractBindings(ReplaceParameters(memberInitialization, memberInitialization.Parameters, new[] { param }));
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
                    Expression memberExpr;
                    if (member is PropertyInfo prop)
                    {
                        memberExpr = Expression.Property(destParam, prop);
                    }
                    else if (member is FieldInfo field)
                    {
                        memberExpr = Expression.Field(destParam, field);
                    }
                    else
                    {
                        throw new ArgumentException("Member should be a field or a property");
                    }

                    var expr = Expression.Assign(memberExpr, m.Expression);
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
                .Where(x => PropertyMappingPredicate(x))
                .Select(x =>
                {
                    //Select Dest property and property values from property mappings
                    object value;
                    var sourceValue = x.Source.GetValue(Source);
                    if (sourceValue != null && !IsSimpleType(sourceValue.GetType()) && deepClone)
                    {
                        //deep clone:
                        value = x.Dest.CanRead ? x.Dest.GetValue(Dest) : null;

                        //Create an instance if dest is null:
                        if (value == null)
                            value = Activator.CreateInstance(x.Dest.PropertyType);
                        PopulateObject(sourceValue, value, PropertyMappingPredicate, true);
                    }
                    else
                    {
                        value = sourceValue;
                    }

                    return new
                    {
                        Dest = x.Dest,
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
            var Props = MapTypes(TIn, TOut).Where(x => PropertyMappingPredicate(x));

            var Binds = Props
                .Where(x => !otherBindings.ContainsKey(x.Dest.Name))  //Ignore explicit bindings on other members
                .Select(P =>
                {
                    var sourceProp = Expression.Property(inputParameter, P.Source);
                    Expression destValue;
                    //Deep cloning:
                    if (!IsSimpleType(P.Source.PropertyType) && deepClone)
                    {
                        var assignments = CloneExpression(P.Source.PropertyType, P.Source.PropertyType, sourceProp, new Dictionary<string, MemberAssignment>(), PropertyMappingPredicate, true);
                        destValue = Expression.MemberInit(Expression.New(P.Dest.PropertyType), assignments);
                    }
                    else
                    {
                        destValue = sourceProp;
                    }

                    return Expression.Bind(P.Dest, destValue);
                })
                .Concat(otherBindings.Values)   //Add member asignments
                .ToArray();

            return Binds;
        }



        public static Expression<Func<T1, TR>> Clone<T1, TR>(Expression<Func<T1, TR>> otherMembers, Func<PropertyMapping, bool> PropertyMappingPredicate, bool deepClone = false) =>
            TypeLambda<T1, TR>(Clone((LambdaExpression)otherMembers, PropertyMappingPredicate, deepClone));
        public static Expression<Func<T1, T2, TR>> Clone<T1, T2, TR>(Expression<Func<T1, T2, TR>> otherMembers, Func<PropertyMapping, bool> PropertyMappingPredicate, bool deepClone = false) => TypeLambda<T1, T2, TR>(Clone((LambdaExpression)otherMembers, PropertyMappingPredicate, deepClone));
        public static Expression<Func<T1, T2, T3, TR>> Clone<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> otherMembers, Func<PropertyMapping, bool> PropertyMappingPredicate, bool deepClone = false) => TypeLambda<T1, T2, T3, TR>(Clone((LambdaExpression)otherMembers, PropertyMappingPredicate, deepClone));
        public static Expression<Func<T1, T2, T3, T4, TR>> Clone<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> otherMembers, Func<PropertyMapping, bool> PropertyMappingPredicate, bool deepClone = false) => TypeLambda<T1, T2, T3, T4, TR>(Clone((LambdaExpression)otherMembers, PropertyMappingPredicate, deepClone));


        /// <summary>
        /// Create an expression that initialize an object of type TOut with all properties of type TIn using the member initizer sintax
        /// The user can override or add new member bindings
        /// </summary>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <param name="PropertyMappingPredicate">After the properties where matched by type and name, a filter with this predicate is applied to the property mappings</param>
        /// <param name="deepClone">True to generate a deep clone expression, false for shallow clone</param>
        /// <returns></returns>
        public static LambdaExpression Clone(
            LambdaExpression otherMembers,
            Func<PropertyMapping, bool> PropertyMappingPredicate,
            bool deepClone = false)
        {
            if (otherMembers == null)
            {
                throw new ArgumentNullException(nameof(otherMembers));
            }
            var outputParams = otherMembers.Parameters.Select(x => Expression.Parameter(x.Type)).ToList();
            var otherBindings = otherMembers == null ? new Dictionary<string, MemberAssignment>() : ExtractBindings(ReplaceParameters(otherMembers, otherMembers.Parameters, outputParams));


            var InputType = otherMembers.Parameters[0].Type;
            var OutputType = otherMembers.ReturnType;

            var binds = CloneExpression(InputType, OutputType, outputParams[0], otherBindings, PropertyMappingPredicate, deepClone);
            var body = Expression.MemberInit(Expression.New(OutputType), binds);

            return Expression.Lambda(body, outputParams);
        }

        public static Expression<Func<T1, TOut>> Clone<T1, TOut>(Expression<Func<T1, TOut>> otherMembers = null)
            where TOut : new()
        {
            Expression<Func<T1, TOut>> def = x => new TOut { };
            return Clone(otherMembers ?? def, x => true, false);
        }

        public static Expression<Func<T1, T2, TOut>> Clone<T1, T2, TOut>(Expression<Func<T1, T2, TOut>> otherMembers = null)
            where TOut : new()
        {
            Expression<Func<T1, T2, TOut>> def = (x0, x1) => new TOut { };
            return Clone(otherMembers ?? def, x => true, false);
        }

        public static Expression<Func<T1, T2, T3, TOut>> Clone<T1, T2, T3, TOut>(Expression<Func<T1, T2, T3, TOut>> otherMembers = null)
            where TOut : new()
        {
            Expression<Func<T1, T2, T3, TOut>> def = (x0, x1, x2) => new TOut { };
            return Clone(otherMembers ?? def, x => true, false);
        }

        public static Expression<Func<T1, T2, T3, T4, TOut>> Clone<T1, T2, T3, T4, TOut>(Expression<Func<T1, T2, T3, T4, TOut>> otherMembers = null)
            where TOut : new()
        {
            Expression<Func<T1, T2, T3, T4, TOut>> def = (x0, x1, x2, x3) => new TOut { };
            return Clone(otherMembers ?? def, x => true, false);
        }

        public static Expression<Func<T1, TOut>> CloneSimple<T1, TOut>(Expression<Func<T1, TOut>> otherMembers = null)
            where TOut : new()
        {
            Expression<Func<T1, TOut>> def = x => new TOut { };
            return Clone(otherMembers ?? def, SimplePropertyMappingPredicate, false);
        }
        public static Expression<Func<T1, T2, TOut>> CloneSimple<T1, T2, TOut>(Expression<Func<T1, T2, TOut>> otherMembers = null)
            where TOut : new()
        {
            Expression<Func<T1, T2, TOut>> def = (x0, x1) => new TOut { };
            return Clone(otherMembers ?? def, SimplePropertyMappingPredicate, false);
        }
        public static Expression<Func<T1, T2, T3, TOut>> CloneSimple<T1, T2, T3, TOut>(Expression<Func<T1, T2, T3, TOut>> otherMembers = null)
        where TOut : new()
        {
            Expression<Func<T1, T2, T3, TOut>> def = (x0, x1, x2) => new TOut { };
            return Clone(otherMembers ?? def, SimplePropertyMappingPredicate, false);
        }
        public static Expression<Func<T1, T2, T3, T4, TOut>> CloneSimple<T1, T2, T3, T4, TOut>(Expression<Func<T1, T2, T3, T4, TOut>> otherMembers = null)
            where TOut : new()
        {
            Expression<Func<T1, T2, T3, T4, TOut>> def = (x0, x1, x2, x3) => new TOut { };
            return Clone(otherMembers ?? def, SimplePropertyMappingPredicate, false);
        }

        /// <summary>
        /// Return a new object by copying all properties that have the same name and type
        /// </summary>
        public static TOut CloneInvoke<TOut, TIn>(TIn input, Expression<Func<TIn, TOut>> otherMembers = null)
            where TOut : new()
        {
            var ret = new TOut();
            PopulateObject(input, ret);
            if (otherMembers != null)
                CopyMembers(input, ret, otherMembers);
            return ret;
        }

        /// <summary>
        /// Return a new object by copying all properties that have the same name and type and that pass the IsSimpleType filter
        /// </summary>
        public static TOut CloneSimpleInvoke<TOut, TIn>(TIn input, Expression<Func<TIn, TOut>> otherMembers = null)
            where TOut : new()
        {
            var ret = new TOut();
            PopulateObjectSimple(input, ret);
            if (otherMembers != null)
                CopyMembers(input, ret, otherMembers);
            return ret;
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
            where TOut : new()
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
            return query.Select(x => CloneInvoke(x, otherMembers));
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
            return query.Select(x => CloneSimpleInvoke(x, otherMembers));
        }

        /// <summary>
        /// Returns true if the type is a value type, a primitive, the type String or a byte array
        /// </summary>
        /// <param name="type">The type to check</param>
        public static bool IsSimpleType(this Type type)
        {
            return
                type.IsValueType ||
                type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(byte[]);
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
