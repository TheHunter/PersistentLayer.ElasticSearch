using System;

namespace PersistentLayer.ElasticSearch.Metadata
{
    public class MetadataEvaluator
    {
        public Func<object, string> Serializer { get; set; }

        public Action<object, object> Merge { get; set; }
    }
}
