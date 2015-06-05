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
    public class MetadataWorker
        : MetadataInfo, IMetadataWorker
    {
        private object emptyReference;
        private MetadataEvaluator evaluator;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataWorker"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="currentStatus">The current status.</param>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="version">The version.</param>
        public MetadataWorker(string id, string indexName, string typeName,
            object currentStatus, MetadataEvaluator evaluator, OriginContext origin, string version)
            : base(id, indexName, typeName, currentStatus, currentStatus, version)
        {
            var originalStatus = Activator.CreateInstance(this.InstanceType, true);
            this.OnInit(evaluator, originalStatus, origin);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataWorker"/> class.
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
        public MetadataWorker(string id, string indexName, string typeName,
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

            this.PreviousStatus = new MetadataInfo(this.Id, this.IndexName, this.TypeName, originalStatus, this.Version);
            this.Origin = origin;

            this.emptyReference = Activator.CreateInstance(this.InstanceType, true);
        }

        /// <summary>
        /// Gets the previous status rappresented in json format.
        /// </summary>
        /// <value>
        /// The previous status.
        /// </value>
        public IMetadataInfo PreviousStatus { get; private set; }

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
            string prevStatus = this.evaluator.Serializer.Invoke(this.PreviousStatus.Instance);
            return !currentStatus.Equals(prevStatus, StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Updates the specified metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        public void Update(IMetadataWorker metadata)
        {
            this.Id = metadata.Id;
            
            //this.evaluator.Merge(this.emptyReference, this.Instance);
            //this.evaluator.Merge(metadata.Instance, this.Instance);
            this.UpdateInstance(metadata.Instance);

            this.Version = metadata.Version;
            this.Origin = metadata.Origin;
        }

        /// <summary>
        /// Restores the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        public void Restore(string version = null)
        {
            var prev = this.PreviousStatus;
            this.PreviousStatus = null;

            // making a reset on the origin reference (all properties can be set to default values.)
            //this.evaluator.Merge(this.emptyReference, this.Instance);
            //this.evaluator.Merge(prev.Instance, this.Instance);
            this.UpdateInstance(prev.Instance);

            if (version != null)
                this.Version = version;
        }

        private void UpdateInstance(object data)
        {
            this.evaluator.Merge(this.emptyReference, this.Instance);
            this.evaluator.Merge(data, this.Instance);
        }

        /// <summary>
        /// Becomes the metadata persistent.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="ArgumentNullException">version;Version for updating metadata cannot be null.</exception>
        public void BecomePersistent(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentNullException("version", "Version for updating metadata cannot be null.");

            if (this.Origin == OriginContext.Newone)
            {
                this.Origin = OriginContext.Storage;
                // here It's needed to set the _idsession property to null.
            }
            this.Version = version;
        }
    }
}
