using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Rappresents a document mapper container used for retreiving info about how come documents should be saved.
    /// </summary>
    public class MapperDescriptorResolver
        : IComponentResolver<IDocumentMapBuilder>
    {
        private readonly HashSet<IDocumentMapBuilder> mappers;

        public MapperDescriptorResolver()
        {
            this.mappers = new HashSet<IDocumentMapBuilder>();
        }

        public MapperDescriptorResolver Register(IDocumentMapBuilder mapConfiguration)
        {
            if (this.mappers.All(builder => builder.DocumenType != mapConfiguration.DocumenType))
                this.mappers.Add(mapConfiguration);
            
            return this;
        }

        public IDocumentMapBuilder Resolve<TKeyType>()
        {
            return this.Resolve(typeof(TKeyType));
        }

        public IDocumentMapBuilder Resolve(Type keyType)
        {
            return this.mappers.FirstOrDefault(mapper => mapper.DocumenType == keyType);
        }
    }
}
