using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public class MapConfiguration
         : IMapConfiguration
    {
        internal MapConfiguration(Type docType)
        {
            this.DocumenType = docType;
        }

        public MapConfiguration(Type docType, IEnumerable<string> surrogateKey, string id = null, BindingFlags? flags = null)
            : this(docType)
        {
            this.SurrogateKey = flags.HasValue ? surrogateKey.Select(s => docType.GetProperty(s, flags.Value)).ToList()
                : surrogateKey.Select(docType.GetProperty).ToList();

            if (!string.IsNullOrWhiteSpace(id))
            {
                this.Id = docType.GetProperty(id);
            }
        }

        public PropertyInfo Id { get; internal set; }

        public Type DocumenType { get; internal set; }

        public IEnumerable<PropertyInfo> SurrogateKey { get; internal set; }

        public bool HasIdProperty
        {
            get { return this.Id != null; }
        }
    }
}
