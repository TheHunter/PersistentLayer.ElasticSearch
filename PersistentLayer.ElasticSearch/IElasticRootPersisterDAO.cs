namespace PersistentLayer.ElasticSearch
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRootEntity">The type of the root entity.</typeparam>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IElasticRootPersisterDAO<in TRootEntity, TEntity>
        : IRootPersisterDAO<TRootEntity, TEntity>
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {
        void Evict(TEntity entity, string index = null);

        void Flush();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRootEntity">The type of the root entity.</typeparam>
    public interface IElasticRootPersisterDAO<in TRootEntity>
        : IRootPersisterDAO<TRootEntity>
        where TRootEntity : class
    {
        void Evict<TEntity>(TEntity entity, string index = null) where TEntity : class, TRootEntity;

        void Flush();
    }
}
