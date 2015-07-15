using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch
{
    public interface IElasticRootPersisterDAO<in TRootEntity, TEntity>
        : IRootPersisterDAO<TRootEntity, TEntity>
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {

    }


    public interface IElasticRootPersisterDAO<in TRootEntity>
        : IRootPersisterDAO<TRootEntity>
        where TRootEntity : class
    {

    }
}
