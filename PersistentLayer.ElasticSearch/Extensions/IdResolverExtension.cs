using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nest;
using Nest.Resolvers;

namespace PersistentLayer.ElasticSearch.Extensions
{
    public static class IdResolverExtension
    {

        public static bool HasIdProperty<TEntity>(this IdResolver resolver)
        {
            return HasIdProperty(resolver, typeof(TEntity));
        }

        public static bool HasIdProperty(this IdResolver resolver, Type type)
        {
            var prop = GetPropertyInfo(resolver, type);
            return prop != null;
        }

        public static PropertyInfo GetPropertyInfo<TEntity>(this IdResolver resolver)
        {
            return GetPropertyInfo(resolver, typeof(TEntity));
        }

        public static PropertyInfo GetPropertyInfo(this IdResolver resolver, Type type)
        {
            ElasticTypeAttribute elasticTypeAttribute = ElasticAttributes.Type(type);
            if (elasticTypeAttribute != null && !string.IsNullOrWhiteSpace(elasticTypeAttribute.IdProperty))
                return GetPropertyCaseInsensitive(type, elasticTypeAttribute.IdProperty);
            const string propertyName = "Id";
            PropertyInfo propertyCaseInsensitive1 = GetPropertyCaseInsensitive(type, propertyName);
            if (propertyCaseInsensitive1 != (PropertyInfo)null)
                return propertyCaseInsensitive1;
            PropertyInfo propertyCaseInsensitive2 = GetPropertyCaseInsensitive(type, type.Name + propertyName);
            if (propertyCaseInsensitive2 != (PropertyInfo)null)
                return propertyCaseInsensitive2;
            PropertyInfo propertyCaseInsensitive3 = GetPropertyCaseInsensitive(type, type.Name + "_" + propertyName);
            if (propertyCaseInsensitive3 != (PropertyInfo)null)
                return propertyCaseInsensitive3;
            else
                return propertyCaseInsensitive3;
        }

        private static PropertyInfo GetPropertyCaseInsensitive(Type type, string propertyName)

        {
            return type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        }

    }
}
