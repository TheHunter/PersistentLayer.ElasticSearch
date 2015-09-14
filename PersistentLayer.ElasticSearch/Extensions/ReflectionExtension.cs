using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nest;
using PersistentLayer.ElasticSearch.Mapping;

namespace PersistentLayer.ElasticSearch.Extensions
{
    public static class ReflectionExtension
    {
        private static readonly Type FunctionGetter;
        private static readonly Type FunctionSetter;

        static ReflectionExtension()
        {
            FunctionGetter = typeof(Func<,>);
            FunctionSetter = typeof(Action<,>);
        }

        public static PropertyInfo AsPropertyInfo<TInstance>(this Expression<Func<TInstance, object>> expression)
        {
            MemberExpression memberExpr = null;
            var exp = expression as LambdaExpression;
            if (exp == null)
                return null;

            switch (exp.Body.NodeType)
            {
                case ExpressionType.Convert:
                    {
                        memberExpr = ((UnaryExpression)exp.Body).Operand as MemberExpression;
                        break;
                    }
                case ExpressionType.MemberAccess:
                    {
                        memberExpr = exp.Body as MemberExpression;
                        break;
                    }
            }

            if (memberExpr == null)
                return null;

            var property = memberExpr.Member as PropertyInfo;
            var docType = typeof(TInstance);

            if (property != null && property.DeclaringType != null && property.DeclaringType != docType)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.CreateInstance
                        | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty;

                property = property.DeclaringType.GetProperty(property.Name, flags);
            }

            return property;
        }

        public static ElasticProperty AsElasticProperty<TInstance>(this Expression<Func<TInstance, object>> docExpression, ElasticInferrer inferrer)
        {
            var property = docExpression.AsPropertyInfo();

            return new ElasticProperty(property,
                inferrer.PropertyName(property),
                instance => docExpression.Compile().Invoke(instance as dynamic));
        }

        /// <summary>
        /// Makes the getter.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        public static Delegate MakeGetter(this PropertyInfo property)
        {
            Type funcType = FunctionGetter.MakeGenericType(property.DeclaringType, property.PropertyType);
            MethodInfo getterMethod = property.GetGetMethod() ?? property.GetGetMethod(true);
            Delegate getter = Delegate.CreateDelegate(funcType, null, getterMethod);
            return getter;
        }

        /// <summary>
        /// Makes the setter.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        public static Delegate MakeSetter(this PropertyInfo property)
        {
            Type actType = FunctionSetter.MakeGenericType(property.DeclaringType, property.PropertyType);
            MethodInfo setterMethod = property.GetSetMethod() ?? property.GetSetMethod(true);
            Delegate setter = Delegate.CreateDelegate(actType, setterMethod);
            return setter;
        }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static object GetDefaultValue(this Type type)
        {
            if (type.Name.StartsWith("Nullable") && type.IsGenericType)
                type = type.GetGenericArguments().First();

            var ret = Activator.CreateInstance(type, true);
            return ret;
        }

        /// <summary>
        /// Indicates if the given type implements the specified interface types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="interfaceTypes">The interface types.</param>
        /// <returns></returns>
        public static bool Implements(this Type type, params Type[] interfaceTypes)
        {
            var interfaces = type.GetInterfaces();
            return interfaceTypes.All(t => interfaces.FirstOrDefault(current => current == t) != null);
        }

        public static bool IsPrimitiveNullable(this Type type)
        {
            return type.IsGenericType && type.Name.Equals("Nullable`1", StringComparison.InvariantCultureIgnoreCase);
        }

        public static Type TryToUnboxType(this Type type)
        {
            if (!type.IsPrimitiveNullable())
                return type;

            return type.GetGenericArguments()[0];
        }
    }
}
