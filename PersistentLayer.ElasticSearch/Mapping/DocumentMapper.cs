using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nest;
using PersistentLayer.ElasticSearch.Extensions;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// A basically contract for document mapper.
    /// </summary>
    public interface IDocumentMapper
    {
        /// <summary>
        /// Gets the type of the document.
        /// </summary>
        /// <value>
        /// The type of the document.
        /// </value>
        Type DocumentType { get; }

        /// <summary>
        /// Gets the identifier property.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        ElasticProperty Id { get; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        ElasticProperty Version { get; set; }

        /// <summary>
        /// Gets the collection which rappresents the surrogate key for documents.
        /// </summary>
        /// <value>
        /// The surrogate key.
        /// </value>
        IEnumerable<ElasticProperty> SurrogateKey { get; }

        /// <summary>
        /// Gets or sets the type of the key gen.
        /// </summary>
        /// <value>
        /// The type of the key gen.
        /// </value>
        KeyGenType KeyGenType { get; set; }

        /// <summary>
        /// Valids the given instance verifying eventually not nullable properties mapped as surrogate key.
        /// </summary>
        /// <param name="instance">The instance to evaluate.</param>
        /// <returns></returns>
        bool ValidConstraints(object instance);

        /// <summary>
        /// Gets the constraint values from the given instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        IEnumerable<ConstraintValue> GetConstraintValues(object instance);
    }

    public interface IDocumentMapper<TDocument>
        : IDocumentMapper
        where TDocument : class
    {
        ElasticProperty MakeElasticProperty(Expression<Func<TDocument, object>> docExpression);
    }

    public class DocumentMapper
        : IDocumentMapper
    {
        
        public DocumentMapper(Type docType)
        {
            if (docType == null)
                throw new ArgumentNullException("docType", "The document type for this mapper cannot be null.");

            this.DocumentType = docType;
        }

        public Type DocumentType { get; internal set; }

        public ElasticProperty Id { get; internal set; }

        public ElasticProperty Version { get; set; }

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

    public class DocumentMapper<TDocument>
        : DocumentMapper, IDocumentMapper<TDocument>
        where TDocument : class
    {
        private readonly ElasticInferrer inferrer;

        public DocumentMapper(ElasticInferrer inferrer)
            : base(typeof(TDocument))
        {
            this.inferrer = inferrer;
        }

        public ElasticProperty MakeElasticProperty(Expression<Func<TDocument, object>> docExpression)
        {
            return docExpression.AsElasticProperty(this.inferrer);
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

}
