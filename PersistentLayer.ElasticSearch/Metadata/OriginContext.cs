namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Indicates the orgin about the instance.
    /// </summary>
    public enum OriginContext
    {
        /// <summary>
        /// Indicates instances has added by insert operations.
        /// </summary>
        Newone,

        /// <summary>
        /// Indicates instances was loaded by load operations.
        /// </summary>
        Storage
    }
}
