namespace PersistentLayer.ElasticSearch
{
    public interface IElasticRootQueryableDAO<in TRootEntity, TEntity>
        : IRootQueryableDAO<TRootEntity, TEntity>
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {
        TKey GetIdentifier<TKey>(TEntity instance, string index = null);

        bool IsCached(TEntity instance, string index = null);

        bool IsDirty(TEntity instance);

        TEntity Load(object identifier, string index = null);

        bool SessionWithChanges();
    }


    public interface IElasticRootQueryableDAO<in TRootEntity>
        : IRootQueryableDAO<TRootEntity>
        where TRootEntity : class
    {
        TKey GetIdentifier<TEntity, TKey>(TEntity instance, string index = null)
            where TEntity : class;

        bool IsCached<TEntity>(TEntity instance, string index = null)
            where TEntity : class, TRootEntity;

        bool IsDirty<TEntity>(TEntity instance)
            where TEntity : class, TRootEntity;

        TEntity Load<TEntity>(object identifier, string index = null)
            where TEntity : class, TRootEntity;

        bool SessionWithChanges();
    }
}
