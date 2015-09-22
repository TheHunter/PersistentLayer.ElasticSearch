using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Elasticsearch.Net;
using Nest;
using PersistentLayer.ElasticSearch.Cache;
using PersistentLayer.ElasticSearch.Exceptions;
using PersistentLayer.ElasticSearch.Extensions;
using PersistentLayer.ElasticSearch.KeyGeneration;
using PersistentLayer.ElasticSearch.Mapping;
using PersistentLayer.ElasticSearch.Metadata;
using PersistentLayer.ElasticSearch.Proxy;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class ElasticSession
        : IElasticSession
    {
        private const string SessionFieldName = "$idsession";
        private readonly ElasticTransactionProvider transactionProvider;
        private readonly IObjectEvaluator evaluator;
        private readonly MapperDescriptorResolver mapResolver;
        private readonly KeyGeneratorResolver keyStrategyResolver;
        private readonly HashSet<ElasticKeyGenerator> keyGenerators;
        private readonly HashSet<IDocumentMapper> docMappers;
        private readonly CustomIdResolver idResolver;
        private readonly DocumentAdapterResolver adapterResolver;
        private readonly HashSet<SessionCacheImpl> indexLocalCache;

        public ElasticSession(string indexName, ElasticTransactionProvider transactionProvider, IObjectEvaluator evaluator, MapperDescriptorResolver mapResolver, CustomIdResolver idResolver, KeyGeneratorResolver keyStrategyResolver, DocumentAdapterResolver adapterResolver, IElasticClient client)
        {
            this.Id = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            this.Index = indexName;
            this.transactionProvider = transactionProvider;
            this.mapResolver = mapResolver;
            this.keyStrategyResolver = keyStrategyResolver;
            this.Client = client;

            this.idResolver = idResolver;
            this.evaluator = evaluator;

            this.keyGenerators = new HashSet<ElasticKeyGenerator>();
            this.docMappers = new HashSet<IDocumentMapper>(new DocumentMapperComparer());
            this.adapterResolver = adapterResolver;
            this.indexLocalCache = new HashSet<SessionCacheImpl>
            {
                new SessionCacheImpl(indexName, client)
            };

            transactionProvider.OnBeginTransaction(this.OnBeginTransaction);
            transactionProvider.OnCommitTransaction(this.OnEndTransaction);
            transactionProvider.OnRollbackTransaction(this.OnEndTransaction);
        }

        private void OnBeginTransaction()
        {
            foreach (var sessionCacheImpl in this.indexLocalCache.AsParallel())
            {
                foreach (var source in sessionCacheImpl.Metadata)
                {
                    source.AsReadOnly(false);
                }
            }
        }

        private void OnEndTransaction()
        {
            foreach (var sessionCacheImpl in this.indexLocalCache.AsParallel())
            {
                foreach (var source in sessionCacheImpl.Metadata)
                {
                    source.AsReadOnly();
                }
            }
        }

        public string Id { get; private set; }

        public string Index { get; private set; }

        protected bool TranInProgress
        {
            get { return this.transactionProvider.InProgress; }
        }

        public IElasticClient Client { get; private set; }

        public TEntity FindBy<TEntity>(object id, string index = null)
            where TEntity : class
        {
            var idStr = id.ToString();
            var typeName = this.Client.Infer.TypeName<TEntity>();
            var indexName = index ?? this.Index;
            var metadata = this.GetCache(indexName).SingleOrDefault(idStr, typeName);

            if (metadata != null)
                return metadata.Instance as dynamic;

            var request = this.Client.GetAsync<TEntity>(descriptor => descriptor
                .Index(indexName)
                .Type(typeName)
                .Id(idStr)
                .EnableSource()
                );

            var ids = new List<string>
            {
                idStr
            };

            var reqVerifier = this.Client.SearchAsync<TEntity>(descriptor => descriptor
                .Index(indexName)
                .Type(typeName)
                .Query(q => q.Ids(ids))
                .Version()
                .ApplySessionFilter(SessionFieldName, this.Id)
                );

            var firstRequest = request.Result;
            var searchRequest = reqVerifier.Result;

            if (!firstRequest.IsValid || !searchRequest.IsValid || !searchRequest.Hits.Any())
                return null;

            var docMapper = this.GetDocumentMapper<TEntity>(indexName);
            var instance = firstRequest.Source;
            
            firstRequest.OverrideProperties(docMapper, instance);

            if (this.TranInProgress)
                this.GetCache(indexName).Attach(firstRequest.AsMetadata(this.evaluator, OriginContext.Storage, readOnly: !this.TranInProgress));

            return instance;
        }

        public IEnumerable<TEntity> FindBy<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            var list = new List<TEntity>();
            var typeName = this.Client.Infer.TypeName<TEntity>();
            var indexName = index ?? this.Index;

            var idsToSearch = new List<string>();

            var metadata = this.GetCache(indexName).FindMetadata(typeName)
                .ToArray();

            foreach (var id in ids.Select(n => n.ToString()))
            {
                var current = metadata.FirstOrDefault(n => n.Id.Equals(id));
                if (current != null)
                    list.Add(current.Instance as dynamic);
                else
                    idsToSearch.Add(id);
            }

            var response = this.Client.Search<TEntity>(descriptor => descriptor
                .Index(indexName)
                .Type(typeName)
                .Query(queryDescriptor => queryDescriptor.Ids(idsToSearch))
                .Version()
                .ApplySessionFilter(SessionFieldName, this.Id)
                );

            var docMapper = this.GetDocumentMapper<TEntity>(indexName);
            foreach (var hit in response.Hits)
            {
                ////hit.Source.SetPropertyValue(hit.Version, docMapper.Version);
                hit.OverrideProperties(docMapper, hit.Source);
                this.GetCache(indexName).Attach(hit.AsMetadata(this.evaluator, OriginContext.Storage, readOnly: !this.TranInProgress));
                list.Add(hit.Source);
            }

            return list;
        }

        public IEnumerable<TEntity> FindAll<TEntity>(string index = null)
            where TEntity : class
        {
            var indexName = index ?? this.Index;
            var response = this.Client.Search<TEntity>(descriptor => descriptor
                .Index(index ?? this.Index)
                .From(0)
                .Take(20)
                .Version()
                .ApplySessionFilter(SessionFieldName, this.Id)
                );

            var cache = this.GetCache(indexName);
            var docMapper = this.GetDocumentMapper<TEntity>(indexName);

            var docs = new List<TEntity>();
            foreach (var hit in response.Hits)
            {
                var metadata = cache.SingleOrDefault(hit.Id, hit.Type);
                var instance = hit.Source;

                if (metadata == null)
                {
                    hit.OverrideProperties(docMapper, hit.Source);
                    docs.Add(instance);
                    cache.Attach(hit.AsMetadata(this.evaluator, OriginContext.Storage, readOnly: !this.TranInProgress));
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
            var idss = new List<string>(ids.Select(n => n.ToString()));

            // A document can already exist, but is only visible by transaction owner.
            var request = this.Client.Count<TEntity>(descriptor => descriptor
                .Index(index ?? this.Index)
                .Query(q => q.Ids(idss))
                );
            return request.Count == ids.Length;
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
            
            var indexName = index ?? this.Index;
            var typeName = this.Client.Infer.TypeName<TEntity>();
            var docMapper = this.GetDocumentMapper<TEntity>(indexName);
            var id = entity.GetPropertyValue<string>(docMapper.Id);

            if (string.IsNullOrWhiteSpace(id))
            {
                var exists = this.Client.DocumentExists(indexName, typeName, entity, docMapper.GetConstraintValues(entity).ToArray());
                if (exists)
                    throw new DuplicatedInstanceException(
                        string.Format(
                            "Impossible to save the given instance because there's an instance with the given constraint, document type: {0}",
                            typeName));

                switch (docMapper.KeyGenType)
                {
                    case KeyGenType.Assigned:
                        {
                            throw new InvalidOperationException("The given document doesn't have any identifier set.");
                        }
                    case KeyGenType.Identity:
                        {
                            var keyGenerator = this.GetKeyGenerator(indexName, typeName, docMapper.Id);
                            var key = keyGenerator.Next();
                            return this.Save(entity, key, indexName);
                        }
                    case KeyGenType.Native:
                        {
                            object instance = this.AsDocumentSession(entity);

                            var response = this.Client.Index(instance, descriptor => descriptor
                                    .Type(typeName)
                                    .Index(indexName)
                                    );

                            if (!response.Created)
                                throw new BusinessPersistentException("Internal error When session tried to save to given instance.", "Save");

                            response.OverrideProperties(docMapper, entity);
                            this.GetCache(indexName).Attach(response.AsMetadata(this.evaluator, OriginContext.Newone, entity, readOnly: !this.TranInProgress));

                            return entity;
                        }
                    default:
                        {
                            return this.Save(entity, id, indexName);
                        }
                }
            }
            //// so, in this case the document is need to update.
            this.UpdateInstance(entity, id, typeName, indexName);
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
            var indexName = index ?? this.Index;
            var cached = this.GetCache(indexName).SingleOrDefault(id, typeName);

            if (cached != null)
            {
                // an error because the given instance shouldn't be present twice in the same session context.
                if (cached.Instance != entity)
                    throw new DuplicatedInstanceException(string.Format("Impossible to attach the given instance because is already present into session cache, id: {0}, index: {1}", cached.Id, cached.IndexName));
            }
            else
            {
                var response = this.Client.Get<TEntity>(id, indexName);
                if (!response.Found)
                    throw new BusinessPersistentException("Error on retrieving the instance with the given identifier", "UpdateInstance");

                var docMapper = this.GetDocumentMapper<TEntity>(indexName);
                response.OverrideProperties(docMapper, response.Source);
                this.GetCache(indexName).Attach(response.AsMetadata(this.evaluator, OriginContext.Storage, version: version, readOnly: !this.TranInProgress));
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

            var idStr = id.ToString();
            var typeName = this.Client.Infer.TypeName<TEntity>();
            var indexName = index ?? this.Index;
            var cached = this.GetCache(indexName).SingleOrDefault(idStr, typeName);

            if (cached != null)
            {
                if (cached.Instance != entity)
                    throw new DuplicatedInstanceException(string.Format("Impossible to attach the given instance because is already present into session cache, id: {0}, index: {1}", cached.Id, cached.IndexName));
            }
            else
            {
                var res0 = this.Client.DocumentExists<TEntity>(descriptor => descriptor
                    .Id(idStr)
                    .Index(indexName));

                if (res0.Exists)
                    throw new DuplicatedInstanceException(string.Format("Impossible to save the given instance because is already present into storage, id: {0}, index: {1}", id, this.Index));

                var dynInstance = this.AsDocumentSession(entity);

                var response = this.Client.Index(dynInstance, descriptor => descriptor
                    .Id(idStr)
                    .Type(typeName)
                    .Index(indexName));

                if (!response.Created)
                    throw new BusinessPersistentException("Internal error When session tried to save to given instance.", "Save");

                var docMapper = this.GetDocumentMapper<TEntity>(indexName);
                response.OverrideProperties(docMapper, entity);

                this.GetCache(indexName).Attach(response.AsMetadata(this.evaluator, OriginContext.Newone, entity, readOnly: !this.TranInProgress));
            }
            
            return entity;
        }

        public void MakeTransient<TEntity>(string index = null, params TEntity[] entities)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return;

            var indexName = index ?? this.Index;
            this.GetCache(indexName).Detach(entities);
        }

        public void MakeTransient<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            if (!this.TranInProgress)
                return;

            var local = ids.Select(n => n.ToString());
            var indexName = index ?? this.Index;
            this.GetCache(indexName).Detach<TEntity>(local.ToArray());
        }

        public void MakeTransient<TEntity>(Expression<Func<TEntity, bool>> predicate, string index = null)
            where TEntity : class
        {
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
                throw new ExecutionQueryException("The given instance wasn't found on storage.", "RefreshState");

            var docMapper = this.GetDocumentMapper<TEntity>(indexName);
            response.OverrideProperties(docMapper, response.Source);

            if (!this.TranInProgress)
                return response.Source;

            var metadata = this.GetCache(indexName).SingleOrDefault(id, typeName);
            var res = response.AsMetadata(this.evaluator, OriginContext.Storage, readOnly: !this.TranInProgress);

            if (metadata == null)
            {
                this.GetCache(indexName).Attach(res);
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

        public TValue GetIdentifier<TEntity, TValue>(TEntity instance, string index = null)
            where TEntity : class
        {
            var indexName = index ?? this.Index;
            var docMapper = this.GetDocumentMapper<TEntity>(indexName);

            return docMapper.Id != null ? docMapper.Id.GetValue<TValue>(instance) : default(TValue);
        }

        public bool Cached<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            return this.GetCache(index ?? this.Index).Cached<TEntity>(ids.Select(n => n.ToString()).ToArray());
        }

        public bool Cached(string index = null, params object[] instances)
        {
            return this.GetCache(index ?? this.Index).Cached(instances);
        }

        public bool Dirty(params object[] instances)
        {
            return this.indexLocalCache.Any(impl => impl.FindMetadata(instances).All(info => info.HasChanged()));
        }

        public bool Dirty()
        {
            return this.indexLocalCache.Any(impl => impl.Metadata.Any(worker => worker.HasChanged()));
        }

        public void Evict(string index = null, params object[] instances)
        {
            this.GetCache(index ?? this.Index).Detach(instances);
        }

        public void Evict<TEntity>(string index = null, params object[] ids)
            where TEntity : class
        {
            this.GetCache(index ?? this.Index).Detach(ids.Select(n => n.ToString()).ToArray());
        }

        public void Evict()
        {
            foreach (var cache in this.indexLocalCache.AsParallel())
            {
                this.Evict(cache);
            }
        }

        private void Evict(ISessionCache cache)
        {
            var toRemove = cache.Metadata.ToList();

            if (!toRemove.Any())
                return;

            IBulkRequest request = new BulkRequest();

            foreach (var metadata in toRemove)
            {
                if (metadata.Origin == OriginContext.Newone)
                {
                    request.Operations.Add(
                        new BulkDeleteOperation<object>(metadata.Id)
                        {
                            Index = metadata.IndexName,
                            Type = metadata.TypeName,
                            Version = metadata.Version
                        });
                }
            }

            IBulkResponse response = this.Client.Bulk(request);
            if (response.ItemsWithErrors.Any())
                throw new BulkOperationException("There are problems when some instances were processed by clear operation.",
                    response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));

            cache.Clear();
        }

        public void Flush()
        {
            if (!this.TranInProgress)
                return;

            foreach (var cache in this.indexLocalCache.AsParallel())
            {
                this.Flush(cache);
            }
        }

        private void Flush(ISessionCache cache)
        {
            IBulkRequest request = new BulkRequest();
            request.Operations = new List<IBulkOperation>();

            //var metadataToPersist = cache.Metadata.Where(info => info.Origin == OriginContext.Newone || info.HasChanged())
            //    .ToList();

            //if (!metadataToPersist.Any())
            //    return;

            //foreach (var metadata in metadataToPersist)
            foreach (var metadata in cache.Metadata)
            {
                switch (metadata.Action)
                {
                    case PersistenceAction.ToBeDeleted:
                    {
                        var current = new BulkDeleteOperation<object>(metadata.Id)
                        {
                            Index = metadata.IndexName,
                            Type = metadata.TypeName,
                            Version = metadata.Version
                        };
                        request.Operations.Add(current);
                        break;
                    }
                    case PersistenceAction.ToBeUpdated:
                    {
                        /*
                            iT'S OCCURRED TO DO 3 CASES:
                         *  1) When doc needs to be updated only.
                         *  2) When doc needs to become persistent (removing the idsession property on repository server..)
                         *  3) WHen doc needs to make both operations (1 & 2).
                        */

                        var docMapper = this.GetDocumentMapper(metadata.InstanceType, metadata.IndexName);
                        var current = new BulkUpdateOperation<object, object>(metadata.Id)
                        {
                            Index = metadata.IndexName,
                            Type = metadata.TypeName,
                            Version = metadata.Version,
                        };

                        string versionStr = string.Empty;
                        var pars = new Dictionary<string, object>
                        {
                            {"doc", metadata.Instance},
                            {"idsessionname", SessionFieldName}
                        };

                        if (!metadata.HasChanged())
                        {
                            current.VersionType = VersionType.Force;
                        }
                        else
                        {
                            if (docMapper.Version != null)
                            {
                                versionStr = " if (k == version) { v++ }";
                                pars.Add("version", docMapper.Version.ElasticName ?? string.Empty);
                            }
                        }
                        current.Script = string.Format("doc.each {{ k, v ->{0} ctx._source[k] = v }}; ctx._source.remove(idsessionname);", versionStr);
                        current.Params = pars;

                        request.Operations.Add(current);
                        break;
                    }
                }
            }

            #region

            // no operations to persist on storage.
            if (!request.Operations.Any())
                return;

            var response = this.Client.Bulk(request);
            if (response.Errors)
            {
                Exception innerException;
                try
                {
                    this.Restore(cache, response.Items);
                    innerException = null;
                }
                catch (Exception ex)
                {
                    innerException = ex;
                }

                const string message = "There are problems when some instances were processed by clear operation.";
                BulkOperationException exception = innerException == null
                    ? new BulkOperationException(message, response.ItemsWithErrors.Select(item => item.ToDocumentResponse()))
                    : new BulkOperationException(message, innerException, response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));

                throw exception;
            }

            // everything was gone well.. so It's requeried to update metadata with bulk response
            // so Version and Origin (for new instances).
            if (response.Items == null)
                return;

            foreach (var item in response.Items)
            {
                var metadata = cache.SingleOrDefault(item.Id, item.Type);
                if (metadata == null)
                    continue;

                var docMapper = this.GetDocumentMapper(metadata.InstanceType, metadata.IndexName);
                item.OverrideProperties(docMapper, metadata.Instance);
                metadata.MakePersistent(item.Version);
            }
            #endregion
        }

        private void Restore(ISessionCache cache, IEnumerable<BulkOperationResponseItem> instancesToRestore)
        {
            // here It's requeried to restore instances which are persisted correctly.
            Func<IMetadataWorker, BulkOperationResponseItem, bool> func = (info, item) =>
                info.Id.Equals(item.Id, StringComparison.InvariantCulture)
                && info.TypeName.Equals(item.Type)
                && info.IndexName.Equals(item.Index);

            IBulkRequest request = new BulkRequest();

            foreach (var item in instancesToRestore.Where(item => item.IsValid))
            {
                var metadata = cache.Metadata.FirstOrDefault(info => func(info, item));
                if (metadata != null)
                {
                    if (metadata.Origin == OriginContext.Newone)
                    {
                        request.Operations.Add(
                            new BulkDeleteOperation<object>(metadata.Id)
                            {
                                Index = metadata.IndexName,
                                Type = metadata.TypeName,
                                Version = metadata.Version
                            });
                    }
                    else
                    {
                        var previousStatus = metadata.GetPreviousStatus();
                        if (previousStatus != null)
                        {
                            request.Operations.Add(
                            new BulkUpdateOperation<object, object>(metadata.Id)
                            {
                                Index = metadata.IndexName,
                                Type = metadata.TypeName,
                                Version = metadata.Version,
                                Doc = previousStatus,
                                RetriesOnConflict = 2
                            });
                        }
                    }
                }
            }

            var response = this.Client.Bulk(request);
            if (response.Errors)
            {
                throw new BulkOperationException("There are problems when some instances were processed by clear operation.",
                response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));
            }
        }

        private object AsDocumentSession(object instance)
        {
            Type sourceType = instance.GetType();
            var adapter = this.adapterResolver.Resolve(sourceType);

            if (adapter == null)
                throw new BusinessLayerException("The adapter used for document session cannot be null.", "AsDocumentSession");

            var destination = adapter.MergeWith(instance);

            adapter.AdapterType.GetProperty(SessionFieldName)
                .SetValue(destination, this.Id, null);

            return destination;
        }

        private IDocumentMapper GetDocumentMapper(Type docType, string index)
        {
            IDocumentMapper current = this.docMappers.FirstOrDefault(mapper => mapper.DocumentType == docType);

            if (current != null)
                return current;

            var mapperDescriptor = this.mapResolver.Resolve(docType);
            if (mapperDescriptor == null)
            {
                var property = this.idResolver.GetPropertyInfo(docType) ?? this.Client.GetIdPropertyInfoOf(index, docType);

                current = new DocumentMapper(docType)
                {
                    DocumentType = docType,
                    Id = property == null ? null : new ElasticProperty(property, this.Client.Infer.PropertyName(property)),
                    KeyGenType = KeyGenType.Identity
                };
            }
            else
            {
                current = mapperDescriptor.Build();
                this.Client.SetIdFieldMappingOf(current.Id, index, docType);
            }

            this.docMappers.Add(current);

            return current;
        }
        
        private IDocumentMapper GetDocumentMapper<TEntity>(string index) where TEntity : class
        {
            return this.GetDocumentMapper(typeof(TEntity), index);
        }

        private ElasticKeyGenerator GetKeyGenerator(string index, string typeName, ElasticProperty keyProperty)
        {
            var current =
                this.keyGenerators.FirstOrDefault(
                    generator => generator.Index.Equals(index, StringComparison.InvariantCulture)
                        && generator.TypeName.Equals(typeName, StringComparison.InvariantCulture)
                        && generator.KeyType == keyProperty.PropertyType
                    );

            if (current == null && keyProperty != null)
            {
                var keyGenStrategy = this.keyStrategyResolver.Resolve(keyProperty.PropertyType);

                if (keyGenStrategy == null)
                    throw new BusinessPersistentException(string.Format("No key generation strategy was founded, KeyType: {0}", keyProperty.PropertyType.Name), "GetKeyGenerator");

                var lastValue = this.Client.GetMaxValueOf(keyProperty, index, typeName);

                current = new ElasticKeyGenerator(keyGenStrategy, lastValue, index, typeName);
                this.keyGenerators.Add(current);
            }

            return current;
        }

        private ISessionCache GetCache(string indexName)
        {
            var cache =
                this.indexLocalCache.FirstOrDefault(
                    impl => impl.Index.Equals(indexName, StringComparison.InvariantCultureIgnoreCase));

            if (cache == null)
            {
                cache = new SessionCacheImpl(indexName, this.Client);
                this.indexLocalCache.Add(cache);
            }

            return cache;
        }
    }
}
