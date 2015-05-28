using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public class DocumentMapper
        : IDocumentMapper
    {
        public DocumentMapper(Type docType)
        {
            this.DocumenType = docType;
        }

        public Type DocumenType { get; internal set; }

        public ElasticProperty Id { get; internal set; }

        public IEnumerable<ElasticProperty> SurrogateKey { get; internal set; }

        public KeyGenStrategy Strategy { get; internal set; }
    }

    public interface IDocumentMapper
    {
        Type DocumenType { get; }

        ElasticProperty Id { get; }

        IEnumerable<ElasticProperty> SurrogateKey { get; }

        KeyGenStrategy Strategy { get; }
    }
}
