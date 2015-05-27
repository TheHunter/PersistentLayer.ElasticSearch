using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nest;
using Nest.Resolvers;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public class CustomIdResolver
        : IdResolver
    {
        public bool HasIdProperty<TEntity>()
        {
            return this.HasIdProperty(typeof(TEntity));
        }

        public bool HasIdProperty(Type type)
        {
            var prop = this.IdInfo(type);
            return prop != null;
        }

        public PropertyInfo IdInfo<TEntity>()
        {
            return this.IdInfo(typeof(TEntity));
        }

        public PropertyInfo IdInfo(Type type)
        {
            ElasticTypeAttribute elasticTypeAttribute = ElasticAttributes.Type(type);
            if (elasticTypeAttribute != null && !string.IsNullOrWhiteSpace(elasticTypeAttribute.IdProperty))
                return this.GetPropertyCaseInsensitive(type, elasticTypeAttribute.IdProperty);
            const string propertyName = "Id";
            PropertyInfo propertyCaseInsensitive1 = this.GetPropertyCaseInsensitive(type, propertyName);
            if (propertyCaseInsensitive1 != null)
                return propertyCaseInsensitive1;
            PropertyInfo propertyCaseInsensitive2 = this.GetPropertyCaseInsensitive(type, type.Name + propertyName);
            if (propertyCaseInsensitive2 != null)
                return propertyCaseInsensitive2;
            PropertyInfo propertyCaseInsensitive3 = this.GetPropertyCaseInsensitive(type, type.Name + "_" + propertyName);
            if (propertyCaseInsensitive3 != null)
                return propertyCaseInsensitive3;
            else
                return propertyCaseInsensitive3;
        }

        private PropertyInfo GetPropertyCaseInsensitive(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        }
    }
}
