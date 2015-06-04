using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Rappresents a document mapper container used for retreiving info about how come documents should be saved.
    /// </summary>
    public class MapperDescriptorResolver
    {
        private readonly HashSet<IDocumentMapBuilder> mappers;

        public MapperDescriptorResolver()
        {
            this.mappers = new HashSet<IDocumentMapBuilder>();
        }

        public MapperDescriptorResolver Register(IDocumentMapBuilder mapConfiguration)
        {
            this.mappers.Add(mapConfiguration);
            return this;
        }

        public IDocumentMapBuilder Resolve<TDocument>()
            where TDocument : class
        {
            return this.Resolve(typeof(TDocument));
        }

        public IDocumentMapBuilder Resolve(Type documenType)
        {
            return this.mappers.FirstOrDefault(mapper => mapper.DocumenType == documenType);
        }
    }
}
