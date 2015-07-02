using System;

namespace PersistentLayer.ElasticSearch.Proxy
{
    /// <summary>
    /// Rappresents an adapter which will be used replacing original type with an adapter type.
    /// </summary>
    public class DocumentAdapter
    {
        private readonly Action<object, object> mergerAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentAdapter"/> class.
        /// </summary>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="adapterType">Type of the adapter.</param>
        /// <param name="mergerAction">The merger action.</param>
        /// <exception cref="ArgumentNullException">
        /// sourceType
        /// or
        /// adapterType
        /// or
        /// mergerAction
        /// </exception>
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

        /// <summary>
        /// Gets the type of the source.
        /// </summary>
        /// <value>
        /// The type of the source.
        /// </value>
        public Type SourceType { get; private set; }

        /// <summary>
        /// Gets the type of the adapter.
        /// </summary>
        /// <value>
        /// The type of the adapter.
        /// </value>
        public Type AdapterType { get; private set; }

        /// <summary>
        /// Merges the with.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Unmatched types for the source instance.</exception>
        public object MergeWith(object source)
        {
            if (!this.SourceType.IsInstanceOfType(source))
                throw new InvalidOperationException("Unmatched types for the source instance.");

            var destination = Activator.CreateInstance(this.AdapterType, true);
            this.mergerAction.Invoke(source, destination);
            return destination;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this.GetType() == obj.GetType())
                return this.GetHashCode() == obj.GetHashCode();

            return false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.SourceType.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Basic type: {0}, Adapter type: {1}", this.SourceType.Name, this.AdapterType.Name);
        }
    }
}
