namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Status related about an instance present into session context.
    /// </summary>
    public enum PersistenceAction
    {
        /// <summary>
        /// Indicates the metadata was changed, and It's ready for updating process.
        /// </summary>
        ToBeUpdated = 1,

        /// <summary>
        /// Indicates the metadata must be deleted.
        /// </summary>
        ToBeDeleted = 2,
    }
}
