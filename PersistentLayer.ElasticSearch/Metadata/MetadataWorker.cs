using System;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Metadata for storing entity info.
    /// </summary>
    public class MetadataWorker
        : MetadataInfo, IMetadataWorker
    {
        private readonly IObjectEvaluator evaluator;
        private readonly object emptyReference;
        private bool readOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataWorker"/> class.
        /// </summary>
        /// <param name="id">
        /// The identifier.
        /// </param>
        /// <param name="indexName">
        /// Name of the index.
        /// </param>
        /// <param name="typeName">
        /// Name of the type.
        /// </param>
        /// <param name="currentStatus">
        /// The current status.
        /// </param>
        /// <param name="evaluator">
        /// The evaluator.
        /// </param>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <param name="version">
        /// The version.
        /// </param>
        /// <param name="readOnly">
        /// The read Only.
        /// </param>
        public MetadataWorker(string id, string indexName, string typeName,
            object currentStatus, IObjectEvaluator evaluator, OriginContext origin, string version, bool readOnly = true)
            : base(id, indexName, typeName, currentStatus, currentStatus, version)
        {
            if (evaluator == null)
                throw new ArgumentNullException("evaluator", "The evaluator cannot be null.");

            this.evaluator = evaluator;
            this.readOnly = readOnly;
            this.Origin = origin;
            this.emptyReference = Activator.CreateInstance(this.InstanceType, true);

            if (!readOnly)
                this.SetPreviousStatus(this.CloneInstance());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataWorker"/> class.
        /// </summary>
        /// <param name="id">
        /// The identifier.
        /// </param>
        /// <param name="indexName">
        /// Name of the index.
        /// </param>
        /// <param name="typeName">
        /// Name of the type.
        /// </param>
        /// <param name="originalStatus">
        /// The original status.
        /// </param>
        /// <param name="currentStatus">
        /// The current status.
        /// </param>
        /// <param name="evaluator">
        /// The evaluator.
        /// </param>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <param name="version">
        /// The version.
        /// </param>
        /// <param name="readOnly">
        /// The read Only.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// the current status has a different type than original status, verify its type before applying a current status.
        /// </exception>
        public MetadataWorker(string id, string indexName, string typeName,
            object originalStatus, object currentStatus, IObjectEvaluator evaluator, OriginContext origin, string version, bool readOnly = true)
            : base(id, indexName, typeName, originalStatus, currentStatus, version)
        {
            if (originalStatus.GetType() != currentStatus.GetType())
                throw new ArgumentException("the current status has a different type than original status, verify its type before applying a current status.");

            if (evaluator == null)
                throw new ArgumentNullException("evaluator", "The evaluator cannot be null.");

            this.evaluator = evaluator;
            this.readOnly = readOnly;
            this.Origin = origin;
            this.emptyReference = Activator.CreateInstance(this.InstanceType, true);
            this.SetPreviousStatus(originalStatus);
        }

        private object CloneInstance()
        {
            var previousStatus = Activator.CreateInstance(this.InstanceType, true);
            this.evaluator.Merge(this.Instance, previousStatus);
            return previousStatus;
        }

        /// <summary>
        /// Called when [initialize].
        /// </summary>
        /// <param name="previousStatus">
        /// The original status.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// evaluator;The evaluator cannot be null.
        /// </exception>
        private void SetPreviousStatus(object previousStatus = null)
        {
            previousStatus = previousStatus ?? this.CloneInstance();
            this.PreviousStatus = new MetadataInfo(this.Id, this.IndexName, this.TypeName, previousStatus, this.Version);
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
            if (this.readOnly)
                return false;

            // the previous state must be different to null.
            string currentStatus = this.evaluator.Serialize(this.Instance);
            string prevStatus = this.evaluator.Serialize(this.PreviousStatus.Instance);
            return !currentStatus.Equals(prevStatus, StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Updates the specified metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        public void Update(IMetadataWorker metadata)
        {
            if (metadata == null)
                throw new InvalidOperationException("Metadata used for update must be referenced.");

            this.SetPreviousStatus();
            this.UpdateInstance(metadata.Instance);
            this.Id = metadata.Id;
            this.Version = metadata.Version;
            this.Origin = metadata.Origin;
        }

        /// <summary>
        /// Restores the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        public void Restore(string version = null)
        {
            if (this.PreviousStatus == null)
                return;

            var prev = this.PreviousStatus;
            this.PreviousStatus = null;
            this.UpdateInstance(prev.Instance);

            if (version != null)
                this.Version = version;
        }

        /// <summary>
        /// Updates the instance.
        /// </summary>
        /// <param name="data">The data.</param>
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
            }
            this.Version = version;
        }

        /// <summary>
        /// Makes read only this instance due to the argument value.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns>
        /// The <see cref="IMetadataWorker" />.
        /// </returns>
        public IMetadataWorker AsReadOnly(bool value = true)
        {
            this.readOnly = value;
            if (!value)
            {
                //var originalStatus = Activator.CreateInstance(this.InstanceType, true);
                //this.evaluator.Merge(this.Instance, originalStatus);
                //this.PreviousStatus = new MetadataInfo(this.Id, this.IndexName, this.TypeName, originalStatus, this.Version);
                this.PreviousStatus = new MetadataInfo(this.Id, this.IndexName, this.TypeName, this.evaluator.Clone(this.Instance), this.Version);
            }
            return this;
        }

        /// <summary>
        /// Gets the previous status.
        /// </summary>
        /// <returns>returns null if no previous status exists.</returns>
        public object GetPreviousStatus()
        {
            return this.PreviousStatus == null ? null : this.PreviousStatus.Instance;
        }
    }
}
