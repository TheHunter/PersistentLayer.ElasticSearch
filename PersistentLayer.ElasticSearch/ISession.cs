using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PersistentLayer.ElasticSearch
{
    public interface ISession
    {
        Guid Id { get; }

        string Index { get; }

        TEntity FindBy<TEntity>(object id)
            where TEntity : class;

        IEnumerable<TEntity> FindBy<TEntity>(params object[] ids)
            where TEntity : class;

        IEnumerable<TEntity> FindAll<TEntity>()
            where TEntity : class;

        IEnumerable<TEntity> FindAll<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        bool Exists<TEntity>(params object[] ids)
            where TEntity : class;

        bool Exists<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        TResult ExecuteExpression<TEntity, TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr)
            where TEntity : class;

        TEntity MakePersistent<TEntity>(TEntity entity)
            where TEntity : class;

        TEntity Update<TEntity>(TEntity entity, string version = null)
            where TEntity : class;

        TEntity Update<TEntity>(TEntity entity, object id, string version = null)
            where TEntity : class;

        IEnumerable<TEntity> MakePersistent<TEntity>(params TEntity[] entities)
            where TEntity : class;

        TEntity Save<TEntity>(TEntity entity, object id)
            where TEntity : class;

        void MakeTransient<TEntity>(params TEntity[] entities)
            where TEntity : class;

        void MakeTransient<TEntity>(params object[] ids)
            where TEntity : class;

        void MakeTransient<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        TEntity RefreshState<TEntity>(TEntity entity)
            where TEntity : class;

        IPagedResult<TEntity> GetPagedResult<TEntity>(int startIndex, int pageSize, Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        IPagedResult<TEntity> GetIndexPagedResult<TEntity>(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        TEntity UniqueResult<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        bool Cached<TEntity>(params object[] ids)
            where TEntity : class;

        bool Cached(params object[] instances);

        bool Dirty(params object[] instances);

        bool Dirty();

        void Evict(params object[] instances);

        void Evict<TEntity>(params object[] ids)
            where TEntity : class;

        void Evict();

        void Flush();

        ISession ChildSession();
    }
}
