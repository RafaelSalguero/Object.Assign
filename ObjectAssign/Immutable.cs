using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tonic
{
    /// <summary>
    /// Copy properties for immutable objects
    /// </summary>
    public static partial class LinqEx
    {
        internal static PropertyInfo GetPropertyFromPropExpression<T, TProp>(Expression<Func<T, TProp>> expr)
        {
            if (expr.Body is MemberExpression e)
            {
                if (e.Member is PropertyInfo prop)
                {
                    return prop;
                }
                else
                {
                    throw new ArgumentException($"Member {e.Member} is not a property");
                }
            }
            else
            {
                throw new ArgumentException($"Expression {expr} does not represent a property");
            }

        }

        class ImmutableTypeMap
        {
            public ImmutableTypeMap(ConstructorInfo constructor, List<(ParameterInfo cparam, PropertyInfo prop)> paramMap)
            {
                Constructor = constructor;
                ParamMap = paramMap;
            }

            public ConstructorInfo Constructor { get; }
            public List<(ParameterInfo cparam, PropertyInfo prop)> ParamMap { get; }

            public static ImmutableTypeMap FromType(Type type)
            {
                var constructor = type.GetConstructors().OrderByDescending(x => x.GetParameters().Length).First();
                var constructorParams = constructor.GetParameters();
                var properties = type.GetProperties().Where(X => X.CanRead).ToList();

                var paramMatches = constructorParams.Select(cparam =>
                (
                    cparam: cparam,
                    prop: properties.Where(prop => prop.Name.ToLower() == cparam.Name.ToLower()).Single()
                )).ToList();

                return new ImmutableTypeMap(constructor, paramMatches);
            }
        }

        /// <summary>
        /// Return a new instance of T by calling its constructor with more parameters, replacing the given property with the newValue.
        /// This only works if the contructor parameter names match with the propety names of the class, the match ignore casing.
        /// </summary>
        public static T SetImmutable<T, TProp>(T obj, Expression<Func<T, TProp>> property, TProp newValue)
        {
            var type = typeof(T);
            var map = ImmutableTypeMap.FromType(type);
            var prop = GetPropertyFromPropExpression(property);

            var invokeArguments = map.ParamMap.Select(x =>
                x.prop == prop ? (object)newValue :
                x.prop.GetValue(obj))
            .ToArray();

            return (T)map.Constructor.Invoke(invokeArguments);
        }
    }
}
