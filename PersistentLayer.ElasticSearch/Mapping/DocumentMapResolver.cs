using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nest;
using Nest.Resolvers;
using PersistentLayer.ElasticSearch.Extensions;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Rappresents a document mapper container used for retreiving info about how come documents should be saved.
    /// </summary>
    public class DocumentMapResolver
    {
        private readonly HashSet<IDocumentMapper> mappers;
        private readonly IdResolver idResolver = new IdResolver();
        
        private readonly ElasticInferrer inferrer;

        public DocumentMapResolver(ElasticInferrer inferrer)
        {
            var comparer = new DocumentMapperComparer();
            this.mappers = new HashSet<IDocumentMapper>(comparer);
            this.inferrer = inferrer;
        }

        public DocumentMapResolver Register(IDocumentMapper mapConfiguration)
        {
            this.mappers.Add(mapConfiguration);
            return this;
        }

        public IDocumentMapper Resolve<TDocument>()
            where TDocument : class
        {
            return this.Resolve(typeof(TDocument));
        }

        public IDocumentMapper Resolve(Type documenType)
        {
            var current = this.mappers.FirstOrDefault(mapper => mapper.DocumenType == documenType);
            if (current == null)
            {
                // occorre prepararne uno nuovo...
                var property = this.idResolver.GetPropertyInfo(documenType);
                current = new DocumentMapper(documenType)
                {
                    DocumenType = documenType,
                    Id = property == null ? null
                                    : new ElasticProperty(property, this.inferrer.PropertyName(property), instance => property.MakeGetter().DynamicInvoke(instance)),
                    Type = KeyGenType.Native
                };
                this.mappers.Add(current);
            }
            return current;
        }
    }
}
