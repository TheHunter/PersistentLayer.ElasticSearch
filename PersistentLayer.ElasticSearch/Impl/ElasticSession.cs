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
        : ISession
    {
        // DOVRA' essere utilizzato dalla cache che gestisce tutti i metadati.
        //private readonly IdResolver idResolver = new IdResolver();
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
            var typeName = this.Client.Infer.TypeName<TEntity>();
            var metadata = this.localCache.MetadataExpression(infos => 
                infos.FirstOrDefault(info => info.Id == id.ToString()
                    && info.IndexName.Equals(this.Index)
                    && info.TypeName.Equals(typeName)));

            if (metadata != null)
                return metadata.CurrentStatus as dynamic;

            var request = this.Client.Get<TEntity>(id.ToString(), this.Index);
            if (!request.IsValid)
                throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "FindBy");

            if (this.TranInProgress)
                this.localCache.Attach(new MetadataInfo(request.Id, request.Index, request.Type, request.Source, this.serializer, OriginContext.Storage, request.Version));

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
                {
                    list.Add(current.CurrentStatus as dynamic);
                }
                else
                {
                    idsToHit.Add(id);
                }
            }

            var response = this.Client.GetMany<TEntity>(idsToHit, this.Index).Where(n => n.Found).ToArray();
            list.AddRange(response.Select(n => n.Source));

            if (this.TranInProgress)
            {
                foreach (var multiGetHit in response)
                {
                    this.localCache.Attach(
                        new MetadataInfo(multiGetHit.Id, multiGetHit.Index, multiGetHit.Type, multiGetHit.Source, this.serializer, OriginContext.Storage,
                            multiGetHit.Version));
                }
            }

            return list;
        }

        public IEnumerable<TEntity> FindAll<TEntity>()
            where TEntity : class
        {
            var response = this.Client.Search<TEntity>(descriptor => descriptor
                .Index(this.Index)
                .From(0)
                .Take(20));

            var hits =
                    response.Hits.Select(
                        n =>
                            new MetadataInfo(n.Id, n.Index, n.Type, n.Source, this.serializer, OriginContext.Storage,
                                n.Version) as IMetadataInfo).ToArray();

            if (this.TranInProgress)
            {
                this.localCache.Attach(hits);
            }
            return response.Hits.Select(n => n.Source).ToArray();
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
                var response = this.Client.Index(entity, descriptor => descriptor.Index(this.Index));
                if (!response.Created)
                    throw new BusinessPersistentException("Error on saving the given instance", "Save");

                this.localCache.Attach(new MetadataInfo(response.Id, response.Index, response.Type, entity, this.serializer, OriginContext.Newone,
                    response.Version));
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
                if (cached.CurrentStatus != entity)
                    throw new DuplicatedInstanceException(string.Format("Impossible to attach the given instance because is already present into session cache, id: {0}, index: {1}", cached.Id, cached.IndexName));
            }
            else
            {
                var request = this.Client.Get<TEntity>(id, this.Index);
                if (!request.IsValid)
                    throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "FindBy");

                var metadata = new MetadataInfo(request.Id, request.Index, request.Type, request.Source, this.serializer,
                    OriginContext.Storage, version ?? request.Version);

                this.localCache.Attach(metadata);
                metadata.UpdateStatus(entity);
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
            // occorre verificare sempre se id è compatibile con la Id della proprietà
            // se e solo se il documento possiede una proprietà associata all'Id del docuemnto.

            if (!this.TranInProgress)
                return entity;

            var typeName = this.Client.Infer.TypeName<TEntity>();
            var cached = this.localCache.MetadataExpression(infos => infos.FirstOrDefault(info => info.IndexName.Equals(this.Index) && info.TypeName.Equals(typeName) && info.Id.Equals(id.ToString())));

            if (cached != null)
            {
                if (cached.CurrentStatus != entity)
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

                this.localCache.Attach(new MetadataInfo(response.Id, response.Index, response.Type, entity, this.serializer,
                    OriginContext.Newone, response.Version));
            }
            
            return entity;
        }

        public void MakeTransient<TEntity>(params TEntity[] entities)
            where TEntity : class
        {
            /*
            prima occorre cancellare i dati dalla cache, poi successivamente occorre metterli in batch per la cancellazione...
            */

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

            //this.UpdateMetadata<TEntity>(response.Id, entity, response.Version);
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
