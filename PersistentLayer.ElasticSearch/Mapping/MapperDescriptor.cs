using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nest;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Prepares a custom map descriptor for making map configuration.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class MapperDescriptor<TDocument>
        : IDocumentMapBuilder
        where TDocument : class
    {
        private readonly List<Action<DocumentMapper>> actions;
        private readonly ElasticInferrer inferrer;
        private readonly CustomIdResolver idResolver = new CustomIdResolver();
        private readonly Type docType;

        public MapperDescriptor(ElasticInferrer inferrer)
        {
            this.actions = new List<Action<DocumentMapper>>();
            this.inferrer = inferrer;
            this.docType = typeof(TDocument);
        }

        /// <summary>
        /// Identifiers the specified document expression.
        /// </summary>
        /// <param name="docExpression">The document expression.</param>
        /// <returns></returns>
        public MapperDescriptor<TDocument> Id(Expression<Func<TDocument, object>> docExpression)
        {
            this.actions.Add(mapper => mapper.Id = this.MakeProperty(docExpression));
            return this;
        }

        /// <summary>
        /// Surrogates the key.
        /// </summary>
        /// <param name="docExpression">The document expression.</param>
        /// <returns></returns>
        public MapperDescriptor<TDocument> SurrogateKey(params Expression<Func<TDocument, object>>[] docExpression)
        {
            var list = docExpression.Select(this.MakeProperty).ToList();
            this.actions.Add(mapper => mapper.SurrogateKey = list);
            return this;
        }

        public MapperDescriptor<TDocument> SetProperty(Action<IDocumentMapper> action)
        {
            this.actions.Add(action.Invoke);
            return this;
        }

        Type IDocumentMapBuilder.DocumenType
        {
            get { return this.docType; }
        }

        IDocumentMapper IDocumentMapBuilder.Build(KeyGenType keyGenType)
        {
            var ret = new DocumentMapper(typeof(TDocument));
            this.actions.ForEach(action => action.Invoke(ret));
            if (ret.Id == null)
            {
                var property = this.idResolver.GetPropertyInfo(typeof(TDocument));
                if (property != null)
                {
                    ret.Id = new ElasticProperty(property, this.inferrer.PropertyName(property));
                }
            }
            ret.KeyGenType = keyGenType;
            return ret;
        }

        /// <summary>
        /// Makes the property.
        /// </summary>
        /// <param name="docExpression">The document expression.</param>
        /// <returns></returns>
        private ElasticProperty MakeProperty(Expression<Func<TDocument, object>> docExpression)
        {
            var property = docExpression.AsPropertyInfo();

            return new ElasticProperty(property,
                this.inferrer.PropertyName(property),
                instance => docExpression.Compile().Invoke(instance as dynamic));
        }
    }
}
