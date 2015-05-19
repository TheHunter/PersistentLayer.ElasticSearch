namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Rappresents the metadata about an instance present into session cache.
    /// </summary>
    public interface IMetadataWorker
        : IMetadataInfo
    {
        /// <summary>
        /// Gets the previous status rappresented in json format.
        /// </summary>
        /// <value>
        /// The previous status.
        /// </value>
        IMetadataInfo PreviousStatus { get; }

        /// <summary>
        /// Gets the origin of the current instance.
        /// </summary>
        /// <value>
        /// The origin.
        /// </value>
        OriginContext Origin { get; }

        /// <summary>
        /// Determines whether this instance has changed.
        /// </summary>
        /// <returns></returns>
        bool HasChanged();

        /// <summary>
        /// Updates the specified metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <param name="isPersistent">if set to <c>true</c> [is persistent].</param>
        void Update(IMetadataWorker metadata, bool isPersistent = false);

        /// <summary>
        /// Restores the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        void Restore(string version = null);
    }
}
