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
        /// <returns></returns>
        bool Update(IMetadataWorker metadata);

        /// <summary>
        /// Restores this instance with the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        bool Restore(string version = null);

        /// <summary>
        /// Becomes the metadata persistent.
        /// </summary>
        /// <param name="version">The version.</param>
        void BecomePersistent(string version);

        /// <summary>
        /// Makes read only this instance due to the argument value.
        /// </summary>
        /// <param name="value">
        /// if set to <c>true</c> [value].
        /// </param>
        /// <returns>
        /// The <see cref="IMetadataWorker"/>.
        /// </returns>
        IMetadataWorker AsReadOnly(bool value = true);

        /// <summary>
        /// Gets the previous status.
        /// </summary>
        /// <returns>returns null if no previous status exists.</returns>
        object GetPreviousStatus();
    }
}
