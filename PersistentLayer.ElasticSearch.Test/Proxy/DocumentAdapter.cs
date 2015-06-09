using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistentLayer.ElasticSearch.Test.Proxy
{
    public class DocumentAdapter
    {
        private Action<object, object> mergerAction;

        public DocumentAdapter(Type sourceType, Type adapterType, Action<object, object> mergerAction)
        {
            this.SourceType = sourceType;
            this.AdapterType = adapterType;
            this.mergerAction = mergerAction;
        }

        public Type SourceType { get; private set; }

        public Type AdapterType { get; private set; }

        public object MergeWith(object source)
        {
            if (!this.SourceType.IsInstanceOfType(source))
                throw new InvalidOperationException("Unmatched types for the source instance.");

            var destination = Activator.CreateInstance(this.AdapterType, true);
            this.mergerAction.Invoke(source, destination);
            return destination;
        }
    }
}
