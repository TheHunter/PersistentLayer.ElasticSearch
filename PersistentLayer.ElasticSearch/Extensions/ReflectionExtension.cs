using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        public static object GetDefaultValue(this Type type)
        {
            if (type.Name.StartsWith("Nullable") && type.IsGenericType)
                type = type.GetGenericArguments().First();

            var ret = Activator.CreateInstance(type, true);
            return ret;
        }

        public static bool Implements(this Type type, params Type[] interfaceTypes)
        {
            var interfaces = type.GetInterfaces();
            return interfaceTypes.All(t => interfaces.FirstOrDefault(current => current == t) != null);
        }
    }
}
