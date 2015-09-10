using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public class ConstraintValue
    {
        public ConstraintValue(string elasticProperty, string propertyValue)
        {
            if (string.IsNullOrWhiteSpace(elasticProperty))
                throw new ArgumentException("The elastic property cannot be empty or null.", "elasticProperty");

            if (string.IsNullOrWhiteSpace(propertyValue))
                throw new ArgumentException("The property value cannot be empty or null.", "propertyValue");

            this.ElasticProperty = elasticProperty;
            this.PropertyValue = propertyValue;
        }

        public string ElasticProperty { get; private set; }

        public string PropertyValue { get; private set; }
    }

}
