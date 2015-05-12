using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Nest;
using Nest.Resolvers;
using PersistentLayer.ElasticSearch.Cache;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Metadata for storing entity info.
    /// </summary>
    public class MetadataInfo
        : Metadata, IMetadataInfo
    {
        //private Func<object, string> serializer;
        private MetadataEvaluator evaluator;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataInfo"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="currentStatus">The current status.</param>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="version">The version.</param>
        public MetadataInfo(string id, string indexName, string typeName,
            object currentStatus, MetadataEvaluator evaluator, OriginContext origin, string version)
            : base(id, indexName, typeName, currentStatus, currentStatus, version)
        {
            var originalStatus = Activator.CreateInstance(this.InstanceType, true);
            this.OnInit(evaluator, originalStatus, origin);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataInfo"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="originalStatus">The original status.</param>
        /// <param name="currentStatus">The current status.</param>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="version">The version.</param>
        /// <exception cref="System.ArgumentException">the current status has a different type than original status, verify its type before applying a current status.</exception>
        public MetadataInfo(string id, string indexName, string typeName,
            object originalStatus, object currentStatus, MetadataEvaluator evaluator, OriginContext origin, string version)
            : base(id, indexName, typeName, originalStatus, currentStatus, version)
        {
            if (originalStatus.GetType() != currentStatus.GetType())
                throw new ArgumentException("the current status has a different type than original status, verify its type before applying a current status.");

            this.OnInit(evaluator, originalStatus, origin);
        }

        /// <summary>
        /// Called when [initialize].
        /// </summary>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="originalStatus">The original status.</param>
        /// <param name="origin">The origin.</param>
        /// <exception cref="System.ArgumentNullException">evaluator;The evaluator cannot be null.</exception>
        private void OnInit(MetadataEvaluator evaluator, object originalStatus, OriginContext origin)
        {
            if (evaluator == null)
                throw new ArgumentNullException("evaluator", "The evaluator cannot be null.");

            this.evaluator = evaluator;
            this.evaluator.Merge.Invoke(this.Instance, originalStatus);

            this.PreviousStatus = new Metadata(this.Id, this.IndexName, this.TypeName, originalStatus, this.Version);
            this.Origin = origin;
        }

        /// <summary>
        /// Gets the previous status rappresented in json format.
        /// </summary>
        /// <value>
        /// The previous status.
        /// </value>
        public IMetadata PreviousStatus { get; private set; }

        /// <summary>
        /// Gets the origin of the current instance (used for transactions, rollback or commit)
        /// <remarks>If the origin is New so rollback operation must be deleted, otherwise updated on commit.</remarks>
        /// </summary>
        /// <value>
        /// The origin.
        /// </value>
        public OriginContext Origin { get; private set; }

        /// <summary>
        /// Determines whether this instance has changed.
        /// </summary>
        /// <returns></returns>
        public bool HasChanged()
        {
            string currentStatus = this.evaluator.Serializer.Invoke(this.Instance);
            string prevstatus = this.evaluator.Serializer.Invoke(this.PreviousStatus.Instance);
            return !currentStatus.Equals(prevstatus, StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Updates the specified metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <param name="isPersistent">if set to <c>true</c> [is persistent].</param>
        public void Update(IMetadataInfo metadata, bool isPersistent = false)
        {
            this.Id = metadata.Id;
            this.Version = metadata.Version;
            this.Origin = isPersistent ? OriginContext.Storage : OriginContext.Newone;
        }

        /// <summary>
        /// Restores the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        public void Restore(string version = null)
        {
            var prev = this.PreviousStatus;
            this.PreviousStatus = null;

            //this.Instance = prev.Instance;  // qui occorre fare un merge... 
            this.evaluator.Merge(prev.Instance, this.Instance);

            if (version != null)
                this.Version = version;
        }
    }
}
