namespace PersistentLayer.ElasticSearch
{
    /// <summary>
    /// Rappresents a contract which exposes persistent operations on Elatic search storage.
    /// </summary>
    /// <typeparam name="TRootEntity">The type of the root entity.</typeparam>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IElasticRootPersisterDAO<in TRootEntity, TEntity>
        : IRootPersisterDAO<TRootEntity, TEntity>
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {
        /// <summary>
        /// Evicts the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="index">The index.</param>
        void Evict(TEntity entity, string index = null);

        /// <summary>
        /// Flushes this instance rendering persistent all changes present on underlaying session.
        /// </summary>
        void Flush();
    }

    /// <summary>
    /// Rappresents a contract which exposes persistent operations on Elatic search storage.
    /// </summary>
    /// <typeparam name="TRootEntity">The type of the root entity.</typeparam>
    public interface IElasticRootPersisterDAO<in TRootEntity>
        : IRootPersisterDAO<TRootEntity>
        where TRootEntity : class
    {
        /// <summary>
        /// Evicts the specified entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="index">The index.</param>
        void Evict<TEntity>(TEntity entity, string index = null) where TEntity : class, TRootEntity;

        /// <summary>
        /// Flushes this instance rendering persistent all changes present on underlaying session.
        /// </summary>
        void Flush();
    }
}
