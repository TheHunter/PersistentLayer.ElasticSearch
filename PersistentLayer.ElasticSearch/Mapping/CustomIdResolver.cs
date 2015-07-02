using System;
using System.Reflection;
using Nest;
using Nest.Resolvers;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// The custom id resolver.
    /// </summary>
    public class CustomIdResolver
        : IdResolver
    {
        /// <summary>
        /// The has id property.
        /// </summary>
        /// <typeparam name="TEntity">
        /// Type of document.
        /// </typeparam>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool HasIdProperty<TEntity>()
        {
            return this.HasIdProperty(typeof(TEntity));
        }

        /// <summary>
        /// The has id property.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool HasIdProperty(Type type)
        {
            var prop = this.GetPropertyInfo(type);
            return prop != null;
        }

        /// <summary>
        /// The get property info.
        /// </summary>
        /// <typeparam name="TEntity">
        /// Type of document.
        /// </typeparam>
        /// <returns>
        /// The <see cref="PropertyInfo"/>.
        /// </returns>
        public PropertyInfo GetPropertyInfo<TEntity>()
        {
            return this.GetPropertyInfo(typeof(TEntity));
        }

        /// <summary>
        /// The get property info.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The <see cref="PropertyInfo"/>.
        /// </returns>
        public PropertyInfo GetPropertyInfo(Type type)
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
            return propertyCaseInsensitive3;
        }

        /// <summary>
        /// The get property case insensitive.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        /// <returns>
        /// The <see cref="PropertyInfo"/>.
        /// </returns>
        private PropertyInfo GetPropertyCaseInsensitive(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
