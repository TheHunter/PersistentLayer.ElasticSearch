namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Metadta evaluator for comparing / merging metadata.
    /// </summary>
    public interface IObjectEvaluator
    {
        /// <summary>
        /// Merges the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        void Merge(object source, object destination);

        /// <summary>
        /// Serializes the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        string Serialize(object source);

        /// <summary>
        /// Clones the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        object Clone(object source);
    }
}
