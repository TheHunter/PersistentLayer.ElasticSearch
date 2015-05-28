using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Rappresents a basic info about a particolar document property.
    /// </summary>
    public class ElasticProperty
    {
        public ElasticProperty(PropertyInfo property, string elasticName, Func<object, object> valueFunc)
        {
            if (property == null)
                throw new ArgumentNullException("property", "property used for ElasticProperty cannot be null.");

            if (string.IsNullOrWhiteSpace(elasticName))
                throw new ArgumentException("The name related to the given property info cannot be null.", "elasticName");

            if (valueFunc == null)
                throw new ArgumentNullException("valueFunc", "The delegate used for retreiving the value from property cannot be null.");

            this.Property = property;
            this.ElasticName = elasticName;
            this.ValueFunc = valueFunc;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <value>
        /// The property.
        /// </value>
        public PropertyInfo Property { get; private set; }

        /// <summary>
        /// Gets the elasticName of the elastic.
        /// </summary>
        /// <value>
        /// The elasticName of the elastic.
        /// </value>
        public string ElasticName { get; private set; }

        /// <summary>
        /// Gets the value function.
        /// </summary>
        /// <value>
        /// The value function.
        /// </value>
        public Func<object, object> ValueFunc { get; private set; }
    }
}
