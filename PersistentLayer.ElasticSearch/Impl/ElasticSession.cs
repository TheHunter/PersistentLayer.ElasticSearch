using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
        //private readonly JsonSerializerSettings jsonSettings;
        private readonly MetadataCache localCache;
        private readonly MetadataEvaluator evaluator;

        public ElasticSession(string indexName, Func<bool> tranInProgress, JsonSerializerSettings jsonSettings, IElasticClient client)
        {
            this.Id = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            this.Index = indexName;
            this.tranInProgress = tranInProgress;
            //this.jsonSettings = jsonSettings;
            this.Client = client;
            
            Func<object, string> serializer = instance => JsonConvert.SerializeObject(instance, Formatting.None, jsonSettings);
            this.localCache = new MetadataCache(this.Index, client);
            this.evaluator = new MetadataEvaluator()
            {
                Serializer = serializer,
                Merge = (source, dest) => JsonConvert.PopulateObject(serializer(source), dest)
            };
        }

        public string Id { get; private set; }

        public string Index { get; private set; }

        protected bool TranInProgress
        {
            get { return this.tranInProgress.Invoke(); }
        }

        public IElasticClient Client { get; private set; }

        public TEntity FindBy<TEntity>(object id, string index = null)
            where TEntity : class
        {
            var typeName = this.Client.Infer.TypeName<TEntity>();
            var metadata = this.localCache.SingleOrDefault(id.ToString(), typeName, this.Index);

            if (metadata != null)
                return metadata.Instance as dynamic;

            var request = this.Client.Get<TEntity>(id.ToString(), index ?? this.Index);
            if (!request.IsValid)
                throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "FindBy");

            if (this.TranInProgress)
                this.localCache.Attach(request.AsMetadata(this.evaluator, OriginContext.Storage));

            return request.Source;
        }

        public IEnumerable<TEntity> FindBy<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            var list = new List<TEntity>();
            var typeName = this.Client.Infer.TypeName<TEntity>();

            var idsToHit = new List<string>();

            var metadata = this.localCache.FindMetadata(typeName, index ?? this.Index)
                .ToArray();

            foreach (var id in ids.Select(n => n.ToString()))
            {
                var current = metadata.FirstOrDefault(n => n.Id.Equals(id));
                if (current != null)
                    list.Add(current.Instance as dynamic);
                else
                    idsToHit.Add(id);
            }

            var response = this.Client.GetMany<TEntity>(idsToHit, index ?? this.Index).Where(n => n.Found).ToArray();

            foreach (var multiGetHit in response)
            {
                this.localCache.Attach(multiGetHit.AsMetadata(this.evaluator, OriginContext.Storage));
                list.Add(multiGetHit.Source);
            }

            return list;
        }

        public IEnumerable<TEntity> FindAll<TEntity>(string index = null)
            where TEntity : class
        {
            var response = this.Client.Search<TEntity>(descriptor => descriptor
                .Version()
                .Index(index ?? this.Index)
                .From(0)
                .Take(20));

            var docs = new List<TEntity>();
            foreach (var hit in response.Hits)
            {
                var metadata = this.localCache.SingleOrDefault(hit.Id, hit.Type, hit.Index);

                if (metadata == null)
                {
                    docs.Add(hit.Source);
                    this.localCache.Attach(hit.AsMetadata(this.evaluator, OriginContext.Storage));
                }
                else
                {
                    docs.Add(metadata.Instance as dynamic);
                }
            }
            return docs;
        }

        public IEnumerable<TEntity> FindAll<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Exists<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            return this.Client.Count<TEntity>(descriptor => descriptor
                .Index(index ?? this.Index)
                .Query(queryDescriptor => queryDescriptor.Ids(ids.Select(n => n.ToString()))))
                .Count == ids.Length;
        }

        public bool Exists<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TResult ExecuteExpression<TEntity, TResult>(Expression<Func<IQueryable<TEntity>, TResult>> queryExpr, string index = null)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity MakePersistent<TEntity>(TEntity entity, string index = null)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return entity;

            var id = this.Client.Infer.Id(entity);
            var typeName = this.Client.Infer.TypeName<TEntity>();

            if (string.IsNullOrWhiteSpace(id))
            {
                /* da aggiungere la proprieta dinamica */
                var response = this.Client.Index(entity, descriptor => descriptor
                    .Index(index ?? this.Index)
                    .Id(id));

                if (!response.Created)
                    throw new BusinessPersistentException("Error on saving the given instance", "Save");

                //this.localCache.Attach(response.AsMetadata(this.evaluator, OriginContext.Newone, entity));
                this.localCache.Attach(response.AsMetadata(this.evaluator, OriginContext.Newone, this.AsDynamicDoc(entity)));
                return entity;
            }

            this.UpdateInstance(entity, id, typeName);
            return entity;
        }

        public TEntity Update<TEntity>(TEntity entity, string index = null, string version = null)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return entity;

            var id = this.Client.Infer.Id(entity);
            var typeName = this.Client.Infer.TypeName<TEntity>();

            if (string.IsNullOrWhiteSpace(id))
                throw new BusinessPersistentException("Impossible to update the given instance because the identifier is missing.", "Update");

            this.UpdateInstance(entity, id, typeName, index, version);
            return entity;
        }

        public TEntity Update<TEntity>(TEntity entity, object id, string index = null, string version = null)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return entity;

            if (id == null || string.IsNullOrWhiteSpace(id.ToString()))
                throw new BusinessPersistentException("Impossible to update the given instance because the identifier is missing.", "Update");

            var typeName = this.Client.Infer.TypeName<TEntity>();
            this.UpdateInstance(entity, id.ToString().Trim(), typeName, index, version);

            return entity;
        }

        private void UpdateInstance<TEntity>(TEntity entity, string id, string typeName, string index = null, string version = null)
            where TEntity : class
        {
            var cached = this.localCache.SingleOrDefault(id, typeName, index);

            if (cached != null)
            {
                // an error because the given instance shouldn't be present twice in the same session context.
                if (cached.Instance != entity)
                    throw new DuplicatedInstanceException(string.Format("Impossible to attach the given instance because is already present into session cache, id: {0}, index: {1}", cached.Id, cached.IndexName));
            }
            else
            {
                var response = this.Client.Get<TEntity>(id, index ?? this.Index);
                if (!response.Found)
                    throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "UpdateInstance");

                this.localCache.Attach(response.AsMetadata(this.evaluator, OriginContext.Storage, version: version));
            }
        }

        public IEnumerable<TEntity> MakePersistent<TEntity>(string index = null, params TEntity[] entities)
            where TEntity : class
        {
            foreach (var entity in entities)
            {
                this.MakePersistent(entity, index);
            }
            return entities;
        }

        public TEntity Save<TEntity>(TEntity entity, object id, string index = null)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return entity;

            var typeName = this.Client.Infer.TypeName<TEntity>();
            var indexName = index ?? this.Index;
            var cached = this.localCache.SingleOrDefault(id.ToString(), typeName, indexName);

            if (cached != null)
            {
                if (cached.Instance != entity)
                    throw new DuplicatedInstanceException(string.Format("Impossible to attach the given instance because is already present into session cache, id: {0}, index: {1}", cached.Id, cached.IndexName));
            }
            else
            {
                var res0 = this.Client.DocumentExists<TEntity>(descriptor => descriptor
                    .Id(id.ToString())
                    .Index(indexName));

                if (res0.Exists)
                    throw new DuplicatedInstanceException(string.Format("Impossible to save the given instance because is already present into storage, id: {0}, index: {1}", id, this.Index));

                var response = this.Client.Index(entity, descriptor => descriptor
                    .Id(id.ToString())
                    .Type(typeName)
                    .Index(indexName));

                if (!response.Created)
                    throw new BusinessPersistentException("Internal error When session tried to save to given instance.", "Save");

                //this.localCache.Attach(response.AsMetadata(this.evaluator, OriginContext.Newone, entity));
                this.localCache.Attach(response.AsMetadata(this.evaluator, OriginContext.Newone, this.AsDynamicDoc(entity)));
            }
            
            return entity;
        }

        public void MakeTransient<TEntity>(string index = null, params TEntity[] entities)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return;

            this.localCache.Detach(index ?? this.Index, entities);
        }

        public void MakeTransient<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return;

            var local = ids.Select(n => n.ToString());
            this.localCache.Detach<TEntity>(index ?? this.Index, local.ToArray());
        }

        public void MakeTransient<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class
        {
            //if (!this.TranInProgress)
            //    return;

            throw new NotImplementedException();
        }

        public TEntity RefreshState<TEntity>(TEntity entity, string id = null, string index = null)
            where TEntity : class
        {
            id = id ?? this.Client.Infer.Id(entity);
            var typeName = this.Client.Infer.TypeName(entity.GetType());
            var indexName = index ?? this.Index;

            var response = this.Client.Get<TEntity>(descriptor => descriptor
                .Id(id)
                .Type(typeName)
                .Index(indexName)
                );

            if (!response.Found)
                throw new ExecutionQueryException("The given instance wasn't found on storage.", "MakeTransient");

            if (!this.TranInProgress)
                return response.Source;

            var metadata = this.localCache.SingleOrDefault(id, typeName, indexName);
            var res = response.AsMetadata(this.evaluator, OriginContext.Storage);

            if (metadata == null)
            {
                this.localCache.Attach(res);
                return response.Source;
            }

            metadata.Update(res);
            return metadata.Instance as dynamic;
        }

        public IPagedResult<TEntity> GetPagedResult<TEntity>(int startIndex, int pageSize, Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public IPagedResult<TEntity> GetIndexPagedResult<TEntity>(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public TEntity UniqueResult<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Cached<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            return this.localCache.Cached<TEntity>(index ?? this.Index, ids.Select(n => n.ToString()).ToArray());
        }

        public bool Cached(string index = null, params object[] instances)
        {
            return this.localCache.Cached(index ?? this.Index, instances);
        }

        public bool Dirty(params object[] instances)
        {
            return this.localCache.FindMetadata(instances: instances).All(info => info.HasChanged());
        }

        public bool Dirty()
        {
            return this.localCache.MetadataExpression(infos => infos.Any(info => info.HasChanged()));
        }

        public void Evict(string index = null, params object[] instances)
        {
            this.localCache.Detach(index ?? this.Index, instances);
        }

        public void Evict<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            this.localCache.Detach(index ?? this.Index, ids.Select(n => n.ToString()).ToArray());
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

        private object AsDynamicDoc(object instance)
        {
            return instance.AsDynamic(new KeyValuePair<string, object>("_idsession", this.Id));
        }
    }
}
