using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace PersistentLayer.ElasticSearch
{
    public static class Util
    {
        private static Type FunctionGetter;

        static Util()
        {
            FunctionGetter = typeof(Func<,>);
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

            return memberExpr.Member as PropertyInfo;
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
    }
}
