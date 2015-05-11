using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Cache
{
    public class MetadataEvaluator
    {
        public Func<object, string> Serializer { get; set; }

        public Action<object, object> Merge { get; set; }
    }
}
