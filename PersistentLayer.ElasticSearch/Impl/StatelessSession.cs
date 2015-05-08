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
            this.Id = Guid.NewGuid();
        }

        protected ElasticClient Client { get; set; }

        public Guid Id { get; private set; }

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
            return this.Client.Count<TEntity>(descriptor => descriptor.Index(this.Index)
                .Query(queryDescriptor => queryDescriptor.Ids(ids.Select(n => n.ToString()))))
                .Count == ids.Length;
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
            var id = this.Client.Infer.Id(entity);
            if (string.IsNullOrWhiteSpace(id))
            {
                var response = this.Client.Index(entity, descriptor => descriptor.Index(this.Index));
                if (!response.Created)
                    throw new BusinessPersistentException("Error on saving the given instance", "Save");
                return entity;
            }
            else
            {
                IUpdateResponse response =
                    this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity).Id(id).Index(this.Index));

                if (!response.IsValid)
                    throw new BusinessPersistentException("Error on updating the given instance.", "Save");
            }
            return entity;
        }

        public TEntity Update<TEntity>(TEntity entity, string version = null) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity Update<TEntity>(TEntity entity, object id, string version = null) where TEntity : class
        {
            throw new NotImplementedException();
        }

        IEnumerable<TEntity> ISession.MakePersistent<TEntity>(params TEntity[] entities)
        {
            throw new NotImplementedException();
        }

        public TEntity Update<TEntity>(TEntity entity, long? version = null)
            where TEntity : class
        {
            var id = this.Client.Infer.Id(entity);
            if (string.IsNullOrWhiteSpace(id))
                throw new BusinessPersistentException("Impossible to update the given instance because the identifier is missing.", "Update");

            var response = version == null ? this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity))
                : this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity).Version(version.Value));

            if (!response.IsValid)
                throw new BusinessPersistentException("Error on updating the given instance.", "Update");

            return entity;
        }

        public IEnumerable<IPersistenceResult<TEntity>> MakePersistent<TEntity>(params TEntity[] entities) where TEntity : class
        {
            var response = this.Client.Bulk(descriptor =>
                descriptor.IndexMany(entities, (indexDescriptor, entity) => indexDescriptor
                    .Index(this.Index)));

            var items = response.Items.ToArray();
            var list = new List<IPersistenceResult<TEntity>>();
            for (var index = 0; index < items.Length; index++)
            {
                var current = items[index];
                list.Add(
                    new PersistenceResult<TEntity>
                    {
                        Error = current.Error,
                        Id = current.Id,
                        Index = current.Index,
                        PersistenceType = PersistenceType.Create,
                        IsValid = current.IsValid
                    });
            }
            return list;
        }

        public TEntity Save<TEntity>(TEntity entity, object id) where TEntity : class
        {
            var response = this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity).Id(id.ToString()));
            if (!response.IsValid)
                throw new BusinessPersistentException("Error on updating the given instance", "Save");

            return entity;
        }

        public void MakeTransient<TEntity>(params TEntity[] entities) where TEntity : class
        {
            this.Client.Bulk(descriptor =>
                descriptor.DeleteMany(entities, (deleteDescriptor, entity) => deleteDescriptor
                    .Index(this.Index).Document(entity)));
        }

        public void MakeTransient<TEntity>(params object[] ids) where TEntity : class
        {
            var local = ids.Select(n => n.ToString());
            this.Client.Bulk(descriptor =>
                descriptor.DeleteMany(local, (deleteDescriptor, s) => deleteDescriptor.Index(this.Index).Id(s)));
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
            return false;
        }

        public bool Cached(params object[] instances)
        {
            return false;
        }

        public bool Dirty(params object[] instances)
        {
            return false;
        }

        public bool Dirty()
        {
            return false;
        }

        public void Evict(params object[] instances)
        {
        }

        public void Evict<TEntity>(params object[] ids) where TEntity : class
        {
        }

        public void Evict()
        {
        }

        public void Flush()
        {
        }

        public ISession ChildSession()
        {
            throw new NotImplementedException();
        }
    }
}
