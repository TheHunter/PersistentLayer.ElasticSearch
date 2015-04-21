﻿using System;
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
            var typeName = this.Client.Infer.TypeName<TEntity>();
            var metadata = this.localCache.MetadataExpression(infos => 
                infos.FirstOrDefault(info => info.Id == id.ToString() && info.IndexName.Equals(this.Index) && info.TypeName.Equals(typeName))
                );

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
                .Take(20)   // numero di default.
                );

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
                    throw new BusinessPersistentException("Error on saving the given instance", "MakePersistent");

                this.localCache.Attach(new MetadataInfo(response.Id, response.Index, response.Type, entity, this.serializer, OriginContext.Newone,
                    response.Version));
                return entity;
            }
            else
            {
                var cached = this.localCache.MetadataExpression(infos => infos.FirstOrDefault(info => info.IndexName.Equals(this.Index) && info.TypeName.Equals(typeName) && info.Id.Equals(id)));
                IUpdateResponse response;

                if (cached != null)
                {
                    cached.UpdateStatus(entity);
                }
                else
                {
                    response = this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity).Id(id).Index(this.Index));
                    if (!response.IsValid)
                        throw new BusinessPersistentException("Error on updating the given instance.", "MakePersistent");

                    this.localCache.Attach(new MetadataInfo(response.Id, response.Index, response.Type, entity, this.serializer, OriginContext.Storage, response.Version));
                }
            }
            return entity;
        }

        public TEntity Update<TEntity>(TEntity entity, long? version = null)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return entity;

            var id = this.Client.Infer.Id(entity);
            if (string.IsNullOrWhiteSpace(id))
                throw new BusinessPersistentException("Impossible to update the given instance because the identifier is missing.", "Update");

            var response = version == null ? this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity))
                : this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity).Version(version.Value));

            if (!response.IsValid)
                throw new BusinessPersistentException("Error on updating the given instance.", "Update");

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

                if (current.IsValid && this.TranInProgress)
                {
                    this.localCache.Attach(
                        new MetadataInfo(current.Id, current.Index, current.Type, entities[index], this.serializer, OriginContext.Newone, current.Version));
                }
            }
            return list;
        }

        public TEntity MakePersistent<TEntity>(TEntity entity, object id)
            where TEntity : class
        {
            // questa sarebbe un aggiornamento del documento in questione.
            var response = this.Client.Update<TEntity>(descriptor => descriptor.Doc(entity).Id(id.ToString()));
            if (!response.IsValid)
                throw new BusinessPersistentException("Error on updating the given instance", "MakePersistent");

            this.localCache.Attach(new MetadataInfo(id.ToString(), response.Index, response.Type, entity, this.serializer, OriginContext.Storage, response.Version));
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

        //private void UpdateMetadata<TEntity>(string id, object instance, string version)
        //    where TEntity: class
        //{
        //    this.localCache.Detach<TEntity>(id);
        //    this.localCache.Attach(new MetadataInfo(id, instance, this.serializer, OriginContext.Storage, version));
        //}
    }
}
