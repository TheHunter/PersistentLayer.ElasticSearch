using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Nest;
using Newtonsoft.Json;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class StatelessSession
        : ISession
    {
        public StatelessSession(string indexName, ElasticClient client)
        {
            this.Index = indexName;
            this.Client = client;
        }

        protected ElasticClient Client { get; set; }

        public string Index { get; private set; }

        public TEntity FindBy<TEntity>(object id) where TEntity : class
        {
            var request = this.Client.Get<TEntity>(id.ToString(), this.Index);
            if (!request.IsValid)
                throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "FindBy");

            return request.Source;
        }

        public IEnumerable<TEntity> FindBy<TEntity>(params object[] ids) where TEntity : class
        {
            return this.Client.GetMany<TEntity>(ids.Cast<string>(), this.Index).Select(n => n.Source);
        }

        public IEnumerable<TEntity> FindAll<TEntity>() where TEntity : class
        {
            return this.Client.Search<TEntity>(descriptor => descriptor.Index(this.Index))
                .Documents;
        }

        public IEnumerable<TEntity> FindAll<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Exists<TEntity>(params object[] ids) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Exists<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TResult ExecuteExpression<TEntity, TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity MakePersistent<TEntity>(TEntity entity) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPersistenceResult<TEntity>> MakePersistent<TEntity>(params TEntity[] entities) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity MakePersistent<TEntity>(TEntity entity, object id) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public void MakeTransient<TEntity>(params TEntity[] entities) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public void MakeTransient<TEntity>(params object[] ids) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public void MakeTransient<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity RefreshState<TEntity>(TEntity entity) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public IPagedResult<TEntity> GetPagedResult<TEntity>(int startIndex, int pageSize, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public IPagedResult<TEntity> GetIndexPagedResult<TEntity>(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity UniqueResult<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Cached<TEntity>(params object[] ids) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Cached(params object[] instances)
        {
            throw new NotImplementedException();
        }

        public bool Dirty(params object[] instances)
        {
            throw new NotImplementedException();
        }

        public bool Dirty()
        {
            throw new NotImplementedException();
        }

        public void Evict(params object[] instances)
        {
            throw new NotImplementedException();
        }

        public void Evict<TEntity>(params object[] ids) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public void Evict()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public ISession ChildSession()
        {
            throw new NotImplementedException();
        }
    }
}
