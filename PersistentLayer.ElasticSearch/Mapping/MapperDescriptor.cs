using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nest;
using PersistentLayer.ElasticSearch.Extensions;

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
        private readonly List<Action<DocumentMapper<TDocument>>> actions;
        private readonly ElasticInferrer inferrer;
        private readonly CustomIdResolver idResolver = new CustomIdResolver();
        private readonly Type docType;

        public MapperDescriptor(ElasticInferrer inferrer)
        {
            this.actions = new List<Action<DocumentMapper<TDocument>>>();
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
            ////var mapBuilder = this as IDocumentMapBuilder;

            ////this.actions.Add(mapper => mapper.Id = mapBuilder.AsElasticProperty(docExpression));
            this.actions.Add(mapper => mapper.Id = docExpression.AsElasticProperty(this.inferrer));
            return this;
        }

        /// <summary>
        /// Surrogates the key.
        /// </summary>
        /// <param name="docExpression">The document expression.</param>
        /// <returns></returns>
        public MapperDescriptor<TDocument> SurrogateKey(params Expression<Func<TDocument, object>>[] docExpression)
        {
            ////var mapBuilder = this as IDocumentMapBuilder;

            ////var list = docExpression.Select(mapBuilder.AsElasticProperty).ToList();
            var list = docExpression.Select(expression => expression.AsElasticProperty(this.inferrer)).ToList();
            this.actions.Add(mapper => mapper.SurrogateKey = list);
            return this;
        }

        public MapperDescriptor<TDocument> SetProperty(Action<IDocumentMapper<TDocument>> action)
        {
            this.actions.Add(action.Invoke);
            return this;
        }

        Type IDocumentMapBuilder.DocumentType
        {
            get { return this.docType; }
        }

        IDocumentMapper IDocumentMapBuilder.Build(KeyGenType keyGenType)
        {
            var ret = new DocumentMapper<TDocument>(this.inferrer);
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
        
        ////ElasticProperty IDocumentMapBuilder.AsElasticProperty<TDoc>(Expression<Func<TDoc, object>> docExpression)
        ////{
        ////    var property = docExpression.AsPropertyInfo();

        ////    return new ElasticProperty(property,
        ////        this.inferrer.PropertyName(property),
        ////        instance => docExpression.Compile().Invoke(instance as dynamic));
        ////}
    }
}
