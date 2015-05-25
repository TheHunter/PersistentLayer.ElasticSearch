using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PersistentLayer.ElasticSearch
{
    public interface ISession
    {
        TEntity FindBy<TEntity>(object id, string index = null)
            where TEntity : class;

        IEnumerable<TEntity> FindBy<TEntity>(string index = null, params object[] ids)
            where TEntity : class;

        IEnumerable<TEntity> FindAll<TEntity>(string index = null)
            where TEntity : class;

        IEnumerable<TEntity> FindAll<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class;

        bool Exists<TEntity>(string index = null, params object[] ids)
            where TEntity : class;

        bool Exists<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class;

        TResult ExecuteExpression<TEntity, TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr, string index = null)
            where TEntity : class;

        TEntity MakePersistent<TEntity>(TEntity entity, string index = null)
            where TEntity : class;

        TEntity Update<TEntity>(TEntity entity, string index = null, string version = null)
            where TEntity : class;

        TEntity Update<TEntity>(TEntity entity, object id, string index = null, string version = null)
            where TEntity : class;

        IEnumerable<TEntity> MakePersistent<TEntity>(string index = null, params TEntity[] entities)
            where TEntity : class;

        TEntity Save<TEntity>(TEntity entity, object id, string index = null)
            where TEntity : class;

        void MakeTransient<TEntity>(string index = null, params TEntity[] entities)
            where TEntity : class;

        void MakeTransient<TEntity>(string index = null, params object[] ids)
            where TEntity : class;

        void MakeTransient<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class;

        TEntity RefreshState<TEntity>(TEntity entity, string id = null, string index = null)
            where TEntity : class;

        IPagedResult<TEntity> GetPagedResult<TEntity>(int startIndex, int pageSize, Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class;

        IPagedResult<TEntity> GetIndexPagedResult<TEntity>(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class;

        TEntity UniqueResult<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class;

        bool Cached<TEntity>(string index = null, params object[] ids)
            where TEntity : class;

        bool Cached(string index = null, params object[] instances);

        bool Dirty(params object[] instances);

        bool Dirty();

        void Evict(string index = null, params object[] instances);

        void Evict<TEntity>(string index = null, params object[] ids)
            where TEntity : class;

        void Evict();

        void Flush();

        ISession ChildSession();
    }
}
