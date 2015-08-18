using System;

namespace PersistentLayer.ElasticSearch
{
    /// <summary>
    /// Rappresents a contract for accessing data from ElasticSearch engine.
    /// </summary>
    /// <typeparam name="TRootEntity">The type of the root entity.</typeparam>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IElasticRootPagedDAO<in TRootEntity, TEntity>
        : IRootPagedDAO<TRootEntity, TEntity>, IElasticRootPersisterDAO<TRootEntity, TEntity>, IElasticRootQueryableDAO<TRootEntity, TEntity>, IDisposable
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {
    }

    /// <summary>
    /// Rappresents a contract for accessing data ElasticSearch engine.
    /// </summary>
    /// <typeparam name="TRootEntity">The type of the root entity.</typeparam>
    public interface IElasticRootPagedDAO<in TRootEntity>
        : IRootPagedDAO<TRootEntity>, IElasticRootPersisterDAO<TRootEntity>, IElasticRootQueryableDAO<TRootEntity>, IDisposable
        where TRootEntity : class
    {
    }
}
