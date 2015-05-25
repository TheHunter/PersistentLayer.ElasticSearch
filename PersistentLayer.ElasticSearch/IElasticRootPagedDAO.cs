using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch
{
    public interface IElasticRootPagedDAO<in TRootEntity, TEntity>
        : IRootPagedDAO<TRootEntity, TEntity>, IDisposable
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {
    }


    public interface IElasticRootPagedDAO<in TRootEntity>
        : IRootPagedDAO<TRootEntity>, IDisposable
        where TRootEntity : class
    {
    }
}
