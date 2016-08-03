using System;
using System.Collections.Generic;
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
    public static partial class ExpressionExtensions
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
        static Dictionary<Tuple<Type, Type>, Dictionary<string, PropertyMapping>> Mappings = new Dictionary<Tuple<Type, Type>, Dictionary<string, PropertyMapping>>();

        /// <summary>
        /// Map without using the cache
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<string, PropertyMapping> MapTypesSlow(Type Source, Type Dest)
        {
            var SourceProps = Source.GetProperties().ToDictionary(x => x.Name);
            return
                Dest.GetProperties()
                .Where(x => SourceProps.ContainsKey(x.Name))                        //Filter only properties that are both on source and dest
                .Select(x => new { DestProp = x, SourceProp = SourceProps[x.Name] })//Map by name between
                .Where(x => x.SourceProp.PropertyType == x.DestProp.PropertyType)   //Filter out properties with the same name but different type
                .Select(x => new PropertyMapping { Dest = x.DestProp, Source = x.SourceProp })
                .ToDictionary(x => x.Dest.Name);
        }

        /// <summary>
        /// Map all properties that are both in source and dest. The property comparission takes the property name and property type. This method is used internally by the Assign method
        /// </summary>
        /// <param name="Source">Source type</param>
        /// <param name="Dest">Dest type</param>
        /// <returns></returns>
        internal static Dictionary<string, PropertyMapping> MapTypes(Type Source, Type Dest)
        {
            var Key = Tuple.Create(Source, Dest);
            Dictionary<string, PropertyMapping> Result;
            if (!Mappings.TryGetValue(Key, out Result))
            {
                Result = MapTypesSlow(Source, Dest);
                Mappings.Add(Key, Result);
            }
            return Result;
        }

        /// <summary>
        /// Extract a dictionary from bindings from a member initialization expression
        /// </summary>
        /// <typeparam name="TIn">Source type</typeparam>
        /// <typeparam name="TOut">Dest type</typeparam>
        /// <param name="expression">Member initialization expression</param>
        /// <param name="inputParameter">Expression that will replace the input parameter of the given expression</param>
        /// <returns></returns>
        internal static Dictionary<string, MemberAssignment> ExtractBindings<TIn, TOut>(Expression<Func<TIn, TOut>> expression, Expression inputParameter)
        {
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
        /// Create an expression that initialize an object of type TOut with all properties of type TIn using the member initizer sintax
        /// Property mapping between types is done with the MapTypes method.
        /// The user can override or add new member bindings
        /// </summary>
        /// <typeparam name="TIn">The source type</typeparam>
        /// <typeparam name="TOut">The object of the type that will be member initialized</typeparam>
        /// <param name="otherMembers">Override or add new member initialization that are not part of the mapping between types. If null only properties with the same name and type will be assigned</param>
        /// <returns></returns>
        public static Expression<Func<TIn, TOut>> Clone<TIn, TOut>(Expression<Func<TIn, TOut>> otherMembers = null)
        {
            var Props = MapTypes(typeof(TIn), typeof(TOut));

            var Param = Expression.Parameter(typeof(TIn), "cloneInput");
            var OtherBindings = otherMembers == null ? new Dictionary<string, MemberAssignment>() : ExtractBindings(otherMembers, Param);

            var Binds = Props
                .Where(x => !OtherBindings.ContainsKey(x.Key))  //Ignore explicit bindings on other members
                .Select(P => Expression.Bind(P.Value.Dest, Expression.Property(Param, P.Value.Source)))
                .Concat(OtherBindings.Values)   //Add member asignments
                .ToArray();

            var Body = Expression.MemberInit(Expression.New(typeof(TOut)), Binds);

            return Expression.Lambda<Func<TIn, TOut>>(Body, Param);
        }
    }

}
