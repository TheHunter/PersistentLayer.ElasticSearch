using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public class DocumentMapper
        : IDocumentMapper
    {
        public DocumentMapper(Type docType)
        {
            this.DocumentType = docType;
        }

        public Type DocumentType { get; internal set; }

        public ElasticProperty Id { get; internal set; }

        public IEnumerable<ElasticProperty> SurrogateKey { get; internal set; }

        public KeyGenType KeyGenType { get; set; }

        public bool ValidConstraints(object instance)
        {
            if (!this.DocumentType.IsInstanceOfType(instance))
                throw new InvalidOperationException("The given instance is not compatible with this document mapper");

            if (this.SurrogateKey == null || this.SurrogateKey.Any())
                return true;

            return this.SurrogateKey.All(property => property.GetValue(instance) != null);
        }

        public IEnumerable<ConstraintValue> GetConstraintValues(object instance)
        {
            if (!this.DocumentType.IsInstanceOfType(instance))
                throw new InvalidOperationException("The given instance is not compatible with this document mapper");

            if (this.SurrogateKey == null || !this.SurrogateKey.Any())
                return Enumerable.Empty<ConstraintValue>();

            return
                this.SurrogateKey.Select(
                    property =>
                    {
                        try
                        {
                            var value = property.GetValue<string>(instance);
                            return new ConstraintValue(property.ElasticName, value);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(string.Format("It was founded an error when It tried to get the property value from instance, property name: {0}", property.ElasticName), ex);
                        }
                    }
                        );
        }
    }

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

    /// <summary>
    /// A basically contract for document mapper.
    /// </summary>
    public interface IDocumentMapper
    {
        Type DocumentType { get; }

        ElasticProperty Id { get; }

        IEnumerable<ElasticProperty> SurrogateKey { get; }

        KeyGenType KeyGenType { get; set; }

        bool ValidConstraints(object instance);

        IEnumerable<ConstraintValue> GetConstraintValues(object instance);
    }
}
