using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class EsRootPagedDAO<TRootEntity>
        : IRootPersisterDAO<TRootEntity>
        where TRootEntity : class
    {
        public TEntity FindBy<TEntity>(object identifier) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public bool Exists<TEntity>(object identifier) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public bool Exists<TEntity>(ICollection identifiers) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public bool Exists<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public TEntity UniqueResult<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> FindAll<TEntity>() where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> FindAll<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public TResult ExecuteExpression<TEntity, TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public ITransactionProvider GetTransactionProvider()
        {
            throw new NotImplementedException();
        }

        public TEntity MakePersistent<TEntity>(TEntity entity) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public TEntity MakePersistent<TEntity>(TEntity entity, object identifier) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> MakePersistent<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public void MakeTransient<TEntity>(TEntity entity) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public void MakeTransient<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }
    }

    public class EsRootPagedDAO<TRootEntity, TEntity>
        : IRootPersisterDAO<TRootEntity, TEntity>
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {
        public bool Exists(object identifier)
        {
            throw new NotImplementedException();
        }

        public bool Exists(ICollection identifiers)
        {
            throw new NotImplementedException();
        }

        public bool Exists(Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public TEntity FindBy(object identifier)
        {
            throw new NotImplementedException();
        }

        public TEntity UniqueResult(Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> FindAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> FindAll(Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public TResult ExecuteExpression<TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr)
        {
            throw new NotImplementedException();
        }

        public ITransactionProvider GetTransactionProvider()
        {
            throw new NotImplementedException();
        }

        public TEntity MakePersistent(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public TEntity MakePersistent(TEntity entity, object identifier)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> MakePersistent(IEnumerable<TEntity> entities)
        {
            throw new NotImplementedException();
        }

        public void MakeTransient(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public void MakeTransient(IEnumerable<TEntity> entities)
        {
            throw new NotImplementedException();
        }
    }
}
