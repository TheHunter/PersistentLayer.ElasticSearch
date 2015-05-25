using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class ElasticRootPagedDAO<TRootEntity>
        : IElasticRootPagedDAO<TRootEntity>
        where TRootEntity : class
    {
        private IElasticTransactionProvider provider;

        public ElasticRootPagedDAO(IElasticTransactionProvider provider)
        {
            this.provider = provider;
        }
        
        protected IElasticSession Session
        {
            get { return this.provider.Session; }
        }

        public TEntity FindBy<TEntity>(object identifier) where TEntity : class, TRootEntity
        {
            return this.Session.FindBy<TEntity>(identifier);
        }

        public bool Exists<TEntity>(object identifier) where TEntity : class, TRootEntity
        {
            return this.Session.Exists<TEntity>(ids: identifier);
        }

        public bool Exists<TEntity>(ICollection identifiers) where TEntity : class, TRootEntity
        {
            return this.Session.Exists<TEntity>(ids: identifiers);
        }

        public bool Exists<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, TRootEntity
        {
            return this.Session.Exists(predicate);
        }

        public TEntity UniqueResult<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, TRootEntity
        {
            return this.Session.UniqueResult(predicate);
        }

        public IEnumerable<TEntity> FindAll<TEntity>() where TEntity : class, TRootEntity
        {
            return this.Session.FindAll<TEntity>();
        }

        public IEnumerable<TEntity> FindAll<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, TRootEntity
        {
            return this.Session.FindAll(predicate);
        }

        public TResult ExecuteExpression<TEntity, TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr) where TEntity : class, TRootEntity
        {
            return this.Session.ExecuteExpression(queryExpr);
        }

        public IPagedResult<TEntity> GetPagedResult<TEntity>(int startIndex, int pageSize, Expression<Func<TEntity, bool>> predicate) where TEntity : class, TRootEntity
        {
            throw new NotImplementedException();
        }

        public ITransactionProvider GetTransactionProvider()
        {
            return this.provider;
        }

        public TEntity MakePersistent<TEntity>(TEntity entity) where TEntity : class, TRootEntity
        {
            return this.Session.MakePersistent(entity);
        }

        public TEntity MakePersistent<TEntity>(TEntity entity, object identifier) where TEntity : class, TRootEntity
        {
            return this.Session.Update(entity, identifier);
        }

        public IEnumerable<TEntity> MakePersistent<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, TRootEntity
        {
            return this.Session.MakePersistent(entities: entities.ToArray());
        }

        public void MakeTransient<TEntity>(TEntity entity) where TEntity : class, TRootEntity
        {
            this.Session.MakeTransient(entities: entity);
        }

        public void MakeTransient<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, TRootEntity
        {
            this.Session.MakeTransient(entities: entities);
        }

        public void Dispose()
        {
            this.provider = null;
        }
    }

    public class ElasticRootPagedDAO<TRootEntity, TEntity>
        : IElasticRootPagedDAO<TRootEntity, TEntity>
        where TRootEntity : class
        where TEntity : class, TRootEntity
    {
        private IElasticTransactionProvider provider;

        public ElasticRootPagedDAO(IElasticTransactionProvider provider)
        {
            this.provider = provider;
        }

        protected IElasticSession Session
        {
            get { return this.provider.Session; }
        }

        public bool Exists(object identifier)
        {
            return this.Session.Exists<TEntity>(ids: identifier);
        }

        public bool Exists(ICollection identifiers)
        {
            return this.Session.Exists<TEntity>(ids: identifiers);
        }

        public bool Exists(Expression<Func<TEntity, bool>> predicate)
        {
            return this.Session.Exists(predicate);
        }

        public TEntity FindBy(object identifier)
        {
            return this.Session.FindBy<TEntity>(identifier);
        }

        public TEntity UniqueResult(Expression<Func<TEntity, bool>> predicate)
        {
            return this.Session.UniqueResult(predicate);
        }

        public IEnumerable<TEntity> FindAll()
        {
            return this.Session.FindAll<TEntity>();
        }

        public IEnumerable<TEntity> FindAll(Expression<Func<TEntity, bool>> predicate)
        {
            return this.Session.FindAll(predicate);
        }

        public TResult ExecuteExpression<TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr)
        {
            return this.Session.ExecuteExpression(queryExpr);
        }

        public IPagedResult<TEntity> GetPagedResult(int startIndex, int pageSize, Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public ITransactionProvider GetTransactionProvider()
        {
            return this.provider;
        }

        public TEntity MakePersistent(TEntity entity)
        {
            return this.Session.MakePersistent(entity);
        }

        public TEntity MakePersistent(TEntity entity, object identifier)
        {
            return this.Session.Update(entity, identifier);
        }

        public IEnumerable<TEntity> MakePersistent(IEnumerable<TEntity> entities)
        {
            return this.Session.MakePersistent(entities: entities.ToArray());
        }

        public void MakeTransient(TEntity entity)
        {
            this.Session.MakeTransient(entities: entity);
        }

        public void MakeTransient(IEnumerable<TEntity> entities)
        {
            this.Session.MakeTransient(entities: entities);
        }

        public void Dispose()
        {
            this.provider = null;
        }
    }
}
