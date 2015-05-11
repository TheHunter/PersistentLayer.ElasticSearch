using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Nest;
using Nest.Resolvers;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Cache;
using PersistentLayer.ElasticSearch.Exceptions;
using PersistentLayer.ElasticSearch.Extensions;
using PersistentLayer.ElasticSearch.Metadata;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class ElasticSession
        : IElasticSession
    {
        private readonly Func<bool> tranInProgress;
        private readonly JsonSerializerSettings jsonSettings;
        private readonly Func<object, string> serializer;
        private readonly MetadataCache localCache;

        public ElasticSession(string indexName, Func<bool> tranInProgress, JsonSerializerSettings jsonSettings, ElasticClient client)
        {
            this.Id = Guid.NewGuid();
            this.Index = indexName;
            this.tranInProgress = tranInProgress;
            this.jsonSettings = jsonSettings;
            this.Client = client;
            this.serializer = instance => JsonConvert.SerializeObject(instance, Formatting.None, jsonSettings);
            this.localCache = new MetadataCache(this.Index, client);
        }

        public Guid Id { get; private set; }

        public string Index { get; private set; }

        protected bool TranInProgress
        {
            get { return this.tranInProgress.Invoke(); }
        }

        public IElasticClient Client { get; private set; }

        public TEntity FindBy<TEntity>(object id)
            where TEntity : class
        {
            var typeName = this.Client.Infer.TypeName<TEntity>();
            var metadata = this.localCache.MetadataExpression(infos => 
                infos.FirstOrDefault(info => info.Id == id.ToString()
                    && info.IndexName.Equals(this.Index)
                    && info.TypeName.Equals(typeName)));

            if (metadata != null)
                return metadata.Instance as dynamic;

            var request = this.Client.Get<TEntity>(id.ToString(), this.Index);
            if (!request.IsValid)
                throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "FindBy");

            if (this.TranInProgress)
                this.localCache.Attach(request.AsMetadata(this.serializer, OriginContext.Storage));

            return request.Source;
        }

        public IEnumerable<TEntity> FindBy<TEntity>(params object[] ids)
            where TEntity : class
        {
            var list = new List<TEntity>();
            var typeName = this.Client.Infer.TypeName<TEntity>();

            var idsToHit = new List<string>();

            var metadata = this.localCache.FindMetadata(info => info.IndexName.Equals(this.Index) && info.TypeName.Equals(typeName)).ToArray();
            foreach (var id in ids.Select(n => n.ToString()))
            {
                var current = metadata.FirstOrDefault(n => n.Id.Equals(id));
                if (current != null)
                    list.Add(current.Instance as dynamic);
                else
                    idsToHit.Add(id);
            }

            var response = this.Client.GetMany<TEntity>(idsToHit, this.Index).Where(n => n.Found).ToArray();

            foreach (var multiGetHit in response)
            {
                this.localCache.Attach(multiGetHit.AsMetadata(this.serializer, OriginContext.Storage));
                list.Add(multiGetHit.Source);
            }

            return list;
        }

        public IEnumerable<TEntity> FindAll<TEntity>()
            where TEntity : class
        {
            var response = this.Client.Search<TEntity>(descriptor => descriptor
                .Version()
                .Index(this.Index)
                .From(0)
                .Take(20));

            var docs = new List<TEntity>();
            foreach (var hit in response.Hits)
            {

                var metadata = this.localCache.MetadataExpression(infos =>
                infos.FirstOrDefault(info => info.Id == hit.Id
                    && info.IndexName.Equals(this.Index)
                    && info.TypeName.Equals(hit.Type)));

                if (metadata == null)
                {
                    docs.Add(hit.Source);
                    this.localCache.Attach(hit.AsMetadata(this.serializer, OriginContext.Storage));
                }
                else
                {
                    docs.Add(metadata.Instance as dynamic);
                }
            }
            return docs;
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
            if (!this.TranInProgress)
                return entity;

            var id = this.Client.Infer.Id(entity);
            var typeName = this.Client.Infer.TypeName<TEntity>();

            if (string.IsNullOrWhiteSpace(id))
            {
                /* da aggiungere la proprieta dinamica */
                var response = this.Client.Index(entity, descriptor => descriptor.Index(this.Index).Id(id));
                if (!response.Created)
                    throw new BusinessPersistentException("Error on saving the given instance", "Save");

                this.localCache.Attach(response.AsMetadata(this.serializer, OriginContext.Newone, entity));

                return entity;
            }

            this.UpdateInstance(entity, id, typeName);
            return entity;
        }

        public TEntity Update<TEntity>(TEntity entity, string version = null)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return entity;

            var id = this.Client.Infer.Id(entity);
            var typeName = this.Client.Infer.TypeName<TEntity>();

            if (string.IsNullOrWhiteSpace(id))
                throw new BusinessPersistentException("Impossible to update the given instance because the identifier is missing.", "Update");

            this.UpdateInstance(entity, id, typeName, version);
            return entity;
        }

        public TEntity Update<TEntity>(TEntity entity, object id, string version = null)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return entity;

            if (id == null || string.IsNullOrWhiteSpace(id.ToString()))
                throw new BusinessPersistentException("Impossible to update the given instance because the identifier is missing.", "Update");

            var typeName = this.Client.Infer.TypeName<TEntity>();
            this.UpdateInstance(entity, id.ToString().Trim(), typeName, version);

            return entity;
        }

        private void UpdateInstance<TEntity>(TEntity entity, string id, string typeName, string version = null)
            where TEntity : class
        {
            var cached = this.localCache.MetadataExpression(infos => infos.FirstOrDefault(info => info.IndexName.Equals(this.Index) && info.TypeName.Equals(typeName) && info.Id.Equals(id)));
            if (cached != null)
            {
                // an error because the given instance shouldn't be present twice in the same session context.
                if (cached.Instance != entity)
                    throw new DuplicatedInstanceException(string.Format("Impossible to attach the given instance because is already present into session cache, id: {0}, index: {1}", cached.Id, cached.IndexName));
            }
            else
            {
                var response = this.Client.Get<TEntity>(id, this.Index);
                if (!response.Found)
                    throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "UpdateInstance");

                this.localCache.Attach(response.AsMetadata(this.serializer, OriginContext.Storage));
            }
        }

        public IEnumerable<TEntity> MakePersistent<TEntity>(params TEntity[] entities)
            where TEntity : class
        {
            foreach (var entity in entities)
            {
                this.MakePersistent(entity);
            }
            return entities;
        }

        public TEntity Save<TEntity>(TEntity entity, object id)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return entity;

            var typeName = this.Client.Infer.TypeName<TEntity>();
            var cached = this.localCache.MetadataExpression(infos => infos.FirstOrDefault(info => info.IndexName.Equals(this.Index) && info.TypeName.Equals(typeName) && info.Id.Equals(id.ToString())));

            if (cached != null)
            {
                if (cached.Instance != entity)
                    throw new DuplicatedInstanceException(string.Format("Impossible to attach the given instance because is already present into session cache, id: {0}, index: {1}", cached.Id, cached.IndexName));
            }
            else
            {
                var res0 = this.Client.DocumentExists<TEntity>(descriptor => descriptor.Id(id.ToString()).Index(this.Index));
                if (res0.Exists)
                    throw new DuplicatedInstanceException(string.Format("Impossible to save the given instance because is already present into storage, id: {0}, index: {1}", id, this.Index));

                var response = this.Client.Index(entity, descriptor => descriptor.Id(id.ToString()).Index(this.Index));
                if (!response.Created)
                    throw new BusinessPersistentException("Internal error When session tried to save to given instance.", "Save");

                this.localCache.Attach(response.AsMetadata(this.serializer, OriginContext.Newone, entity));
            }
            
            return entity;
        }

        public void MakeTransient<TEntity>(params TEntity[] entities)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return;

            this.localCache.Detach(entities);
        }

        public void MakeTransient<TEntity>(params object[] ids)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return;

            var local = ids.Select(n => n.ToString());
            this.localCache.Detach<TEntity>(local.ToArray());
        }

        public void MakeTransient<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            //if (!this.TranInProgress)
            //    return;

            throw new NotImplementedException();
        }

        public TEntity RefreshState<TEntity>(TEntity entity, string id = null)
            where TEntity : class
        {
            id = id ?? this.Client.Infer.Id(entity);
            var response = this.Client.Get<TEntity>(descriptor => descriptor.Id(id));
            if (!response.Found)
                throw new ExecutionQueryException("The given instance wasn't found on storage.", "MakeTransient");

            if (!this.TranInProgress)
                return response.Source;

            this.localCache.AttachOrUpdate(response.AsMetadata(this.serializer, OriginContext.Storage));
            return entity;
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
            if (!this.TranInProgress)
                return;
            this.localCache.Flush();
        }
        
        public ISession ChildSession()
        {
            throw new NotImplementedException();
        }
    }
}
