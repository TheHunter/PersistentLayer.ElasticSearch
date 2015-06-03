using System;
using System.Collections.Generic;

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

        public KeyGenType Type { get; internal set; }

        public bool HasIdProperty
        {
            get { return this.Id != null; }
        }
    }

    public interface IDocumentMapper
    {
        Type DocumenType { get; }

        ElasticProperty Id { get; }

        IEnumerable<ElasticProperty> SurrogateKey { get; }

        KeyGenType Type { get; }

        bool HasIdProperty { get; }
    }
}
