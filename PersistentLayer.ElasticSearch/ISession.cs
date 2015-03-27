using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace PersistentLayer.ElasticSearch
{
    public interface ISession
    {

        string Index { get; }

        TEntity FindBy<TEntity>(object id);

        IEnumerable<TEntity> FindBy<TEntity>(params object[] ids);

        IEnumerable<TEntity> FindAll<TEntity>();

        IEnumerable<TEntity> FindAll<TEntity>(Expression<Func<TEntity, bool>> predicate);

        bool Exists<TEntity>(params object[] id);

        bool Exists<TEntity>(Expression<Func<TEntity, bool>> predicate);

        TResult ExecuteExpression<TEntity, TResult>(
            Expression<Func<IQueryable<TEntity>, TResult>> queryExpr);

        TEntity MakePersistent<TEntity>(TEntity entity);

        IEnumerable<TEntity> MakePersistent<TEntity>(params TEntity[] entity);

        TEntity MakePersistent<TEntity>(TEntity entity, object identifier);

        void MakeTransient<TEntity>(params TEntity[] entity);

        void MakeTransient<TEntity>(params object[] ids);

        void MakeTransient<TEntity>(Expression<Func<TEntity, bool>> predicate);

        TEntity RefreshState<TEntity>(TEntity entity);

        IEnumerable<TEntity> RefreshState<TEntity>(IEnumerable<TEntity> entities);

        IPagedResult<TEntity> GetPagedResult<TEntity>(int startIndex, int pageSize,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        IPagedResult<TEntity> GetIndexPagedResult<TEntity>(int pageIndex, int pageSize,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        TEntity UniqueResult<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        TEntity Merge<TEntity>(TEntity instance);

        bool Cached<TEntity>(params object[] ids);

        bool Cached(params object[] instances);

        bool Dirty(params object[] instances);

        bool SessionWithChanges();

        void Restore(params object[] instances);

        void Evict(params object[] instances);

        void Evict<TEntity>(params object[] ids);

        void Evict<TEntity>(Expression<Func<TEntity, bool>> predicate);

        void Evict();


        ISession ChildSession();
    }
}
