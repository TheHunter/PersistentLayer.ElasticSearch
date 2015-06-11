using System;

namespace PersistentLayer.ElasticSearch.Proxy
{
    public class DocumentAdapter
    {
        private readonly Action<object, object> mergerAction;

        public DocumentAdapter(Type sourceType, Type adapterType, Action<object, object> mergerAction)
        {
            if (sourceType == null)
                throw new ArgumentNullException("sourceType");

            if (adapterType == null)
                throw new ArgumentNullException("adapterType");

            if (mergerAction == null)
                throw new ArgumentNullException("mergerAction");

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

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this.GetType() == obj.GetType())
                return this.GetHashCode() == obj.GetHashCode();

            return false;
        }

        public override int GetHashCode()
        {
            return this.SourceType.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Basic type: {0}, Adapter type: {1}", this.SourceType.Name, this.AdapterType.Name);
        }
    }
}
