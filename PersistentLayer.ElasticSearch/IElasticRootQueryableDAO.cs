using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch
{
    public interface IElasticRootQueryableDAO<in TRootEntity, in TEntity>
        : IDisposable
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {
        ////bool SessionWithChanges();

        ////bool IsCached(TEntity instance, string index = null);

        ////bool IsDirty(TEntity instance, string index = null);
    }

    public interface IElasticRootQueryableDAO<in TRootEntity>
        : IDisposable
        where TRootEntity : class
    {
        ////bool SessionWithChanges();

        ////bool IsCached<TEntity>(TEntity instance, string index = null) where TEntity : class, TRootEntity;

        ////bool IsDirty<TEntity>(TEntity instance, string index = null) where TEntity : class, TRootEntity;
    }
}
