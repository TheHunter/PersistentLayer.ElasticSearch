using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Nest;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Cache;
using PersistentLayer.ElasticSearch.Metadata;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class ElasticSession
        : ISession
    {
        private readonly Func<bool> tranInProgress;
        private readonly JsonSerializerSettings jsonSettings;
        private readonly Func<object, string> serializer;
        private readonly IMetadataCache localCache;

        public ElasticSession(string indexName, Func<bool> tranInProgress, JsonSerializerSettings jsonSettings, ElasticClient client)
        {
            this.Index = indexName;
            this.tranInProgress = tranInProgress;
            this.jsonSettings = jsonSettings;
            this.Client = client;
            this.serializer = instance => JsonConvert.SerializeObject(instance, Formatting.None, jsonSettings);
            this.localCache = null;
        }

        public string Index { get; private set; }

        protected bool TranInProgress
        {
            get { return this.tranInProgress.Invoke(); }
        }

        protected ElasticClient Client { get; set; }

        public TEntity FindBy<TEntity>(object id)
            where TEntity : class
        {
            var request = this.Client.Get<TEntity>(id.ToString(), this.Index);
            if (!request.IsValid)
                throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "FindBy");

            IMetadataInfo metadata = new MetadataInfo(request.Id, request.Source, this.serializer, OriginContext.Storage, request.Version);
            this.localCache.Attach(metadata);

            return request.Source;
        }

        public IEnumerable<TEntity> FindBy<TEntity>(params object[] ids)
            where TEntity : class
        {
            var response = this.Client.GetMany<TEntity>(ids.Cast<string>(), this.Index);
            var list = new List<TEntity>();
            if (this.TranInProgress)
            {
                foreach (var multiGetHit in response)
                {
                    list.Add(multiGetHit.Source);
                    this.localCache.Attach(
                        new MetadataInfo(multiGetHit.Id, multiGetHit.Source, this.serializer, OriginContext.Storage,
                            multiGetHit.Version));
                }
            }
            return list;
        }

        public IEnumerable<TEntity> FindAll<TEntity>()
            where TEntity : class
        {
            return this.Client.Search<TEntity>(descriptor => descriptor.Index(this.Index))
                .Documents;
        }

        public IEnumerable<TEntity> FindAll<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Exists<TEntity>(params object[] ids)
            where TEntity : class
        {
            return this.Client.Count<TEntity>(descriptor => descriptor.Index(this.Index)
                .Query(queryDescriptor => queryDescriptor.Ids(ids.Select(n => n.ToString()))))
                .Count == ids.Length;
        }

        public bool Exists<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TResult ExecuteExpression<TEntity, TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity MakePersistent<TEntity>(TEntity entity)
            where TEntity : class
        {
            var id = this.Client.Infer.Id(entity);
            if (string.IsNullOrWhiteSpace(id))
            {
                var response = this.Client.Index(entity, descriptor => descriptor.Index(this.Index));
                if (!response.Created)
                    throw new BusinessPersistentException("Error on saving the given instance", "MakePersistent");

                this.localCache.Attach(new MetadataInfo(response.Id, entity, this.serializer, OriginContext.Newone,
                    response.Version));
                return entity;
            }
            else
            {
                var cached = this.localCache.MetadataExpression(infos => infos.FirstOrDefault(info => info.Id == id && info.GetType() == typeof(TEntity)));

                IUpdateResponse response = cached == null ? this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity).Id(id).Index(this.Index))
                    : this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity).Id(id).Index(this.Index).Version(Convert.ToInt64(cached.Version)));

                if (!response.IsValid)
                    throw new BusinessPersistentException("Error on updating the given instance.", "MakePersistent");

                this.localCache.Detach<TEntity>(id);
                this.localCache.Attach(new MetadataInfo(id, entity, this.serializer, OriginContext.Newone,
                    response.Version));
            }
            return entity;
        }

        public TEntity Update<TEntity>(TEntity entity, long? version = null)
        {


            return entity;
        }

        public IEnumerable<IPersistenceResult<TEntity>> MakePersistent<TEntity>(params TEntity[] entities)
            where TEntity : class
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
                if (current.IsValid)
                {
                    this.localCache.Attach(
                        new MetadataInfo(current.Id, entities[index], this.serializer, OriginContext.Newone, current.Version));
                }
            }
            return list;
        }

        public TEntity MakePersistent<TEntity>(TEntity entity, object id)
            where TEntity : class
        {
            var response = this.Client.Index(entity, descriptor => descriptor.Index(this.Index).Id(id.ToString()));
            if (response.Created)
            {
                this.localCache.Attach(new MetadataInfo(id.ToString(), entity, this.serializer, OriginContext.Newone, response.Version));
            }
            // otherwise error...
            return entity;
        }

        public void MakeTransient<TEntity>(params TEntity[] entities)
            where TEntity : class
        {
            var response = this.Client.Bulk(descriptor =>
                descriptor.DeleteMany(entities, (deleteDescriptor, entity) => deleteDescriptor
                    .Index(this.Index).Document(entity)));

            this.localCache.Detach(entities);
        }

        public void MakeTransient<TEntity>(params object[] ids)
            where TEntity : class
        {
            var local = ids.Select(n => n.ToString());
            var response = this.Client.Bulk(descriptor => 
                descriptor.DeleteMany(local, (deleteDescriptor, s) => deleteDescriptor.Index(this.Index).Id(s)));

            this.localCache.Detach<TEntity>(local.ToArray());
        }

        public void MakeTransient<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity RefreshState<TEntity>(TEntity entity)
            where TEntity : class
        {
            var response = this.Client.Get<TEntity>(descriptor => descriptor.IdFrom(entity));
            if (!response.Found)
                throw new ExecutionQueryException("The given instance wasn't found on storage.", "MakeTransient");

            this.localCache.Detach<TEntity>(response.Id);
            this.localCache.Attach(new MetadataInfo(response.Id, response.Source, this.serializer, OriginContext.Storage,
                response.Version));

            return response.Source;
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

        public bool Cached<TEntity>(params object[] ids)
            where TEntity : class
        {
            return this.localCache.Cached<TEntity>(ids.Select(n => n.ToString()).ToArray());
        }

        public bool Cached(params object[] instances)
        {
            return this.localCache.Cached(instances);
        }

        public bool Dirty(params object[] instances)
        {
            return this.localCache.FindMetadata(instances).All(info => info.HasChanged());
        }

        public bool Dirty()
        {
            return this.localCache.MetadataExpression(infos => infos.Any(info => info.HasChanged()));
        }

        public void Evict(params object[] instances)
        {
            this.localCache.Detach(instances);
        }

        public void Evict<TEntity>(params object[] ids)
            where TEntity : class
        {
            this.localCache.Detach(ids.Select(n => n.ToString()).ToArray());
        }

        public void Evict()
        {
            this.localCache.Clear();
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
