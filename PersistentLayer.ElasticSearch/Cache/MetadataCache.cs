﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nest;
using Nest.Resolvers;
using PersistentLayer.ElasticSearch.Exceptions;
using PersistentLayer.ElasticSearch.Extensions;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Cache
{
    public class MetadataCache
        : IMetadataCache, IDisposable
    {
        //private readonly IdResolver idResolver = new IdResolver();
        private readonly HashSet<IMetadataInfo> localCache;
        private readonly IElasticClient client;
        private readonly MetadataComparer comparer;
        private bool disposed;

        public MetadataCache(string index, IElasticClient client)
        {
            this.Index = index;
            this.comparer = new MetadataComparer();
            this.localCache = new HashSet<IMetadataInfo>(this.comparer);
            this.client = client;
            this.disposed = false;
        }

        public string Index { get; private set; }

        public bool Cached<TEntity>(params string[] ids) where TEntity : class
        {
            this.ThrowIfDisposed();

            Type instanceType = typeof(TEntity);
            return this.Cached(instanceType, ids);
        }

        public bool Cached(Type instanceType, params string[] ids)
        {
            var typeName = this.client.Infer.IndexName(instanceType);
            return this.Cached(typeName, ids);
        }

        public bool Cached(string typeName, params string[] ids)
        {
            this.ThrowIfDisposed();

            var toInspect = this.GetCache().ToList();
            return ids.All(s => toInspect.Any(info =>
                info.Id.Equals(s, StringComparison.InvariantCulture)
                && info.TypeName.Equals(typeName, StringComparison.InvariantCulture)));
        }

        public bool Cached(params object[] instances)
        {
            this.ThrowIfDisposed();

            var inferrer = this.client.Infer;
            var toInspect = this.GetCache().ToList();
            return instances.All(instance => toInspect
                .Any(info => 
                    info.TypeName.Equals(inferrer.TypeName(instance.GetType()), StringComparison.InvariantCulture)
                    && info.Instance == instance));
        }

        public IEnumerable<IMetadataInfo> FindMetadata(params object[] instances)
        {
            this.ThrowIfDisposed();

            var toInspect = this.GetCache().ToList();
            return instances.Select(instance => toInspect.FirstOrDefault(info => info.Instance == instance))
                .Where(info => info != null)
                .ToList();
        }

        public IEnumerable<IMetadataInfo> FindMetadata(Expression<Func<IMetadataInfo, bool>> exp)
        {
            this.ThrowIfDisposed();
            return this.GetCache(cond: exp.Compile())
                .ToList();
        }

        public TResult MetadataExpression<TResult>(Expression<Func<IEnumerable<IMetadataInfo>, TResult>> expr)
        {
            this.ThrowIfDisposed();
            return expr.Compile().Invoke(this.GetCache());
        }

        public bool Attach(params IMetadataInfo[] metadata)
        {
            this.ThrowIfDisposed();

            bool ret = true;
            metadata.All(info =>
            {
                ret = ret && this.localCache.Add(info);
                return true;
            });
            return ret;
        }

        public bool AttachOrUpdate(params IMetadataInfo[] metadata)
        {
            this.ThrowIfDisposed();

            foreach (var current in metadata)
            {
                var cur = this.localCache.FirstOrDefault(info => this.comparer.Equals(info, current));
                if (cur == null)
                    this.localCache.Add(current);
                else
                    cur.Update(current);
            }

            return true;
        }

        public bool Detach<TEntity>(params string[] ids) where TEntity : class
        {
            this.ThrowIfDisposed();

            return this.Detach(this.client.Infer.TypeName<TEntity>(), ids);
        }

        public bool Detach(Type instanceType, params string[] ids)
        {
            this.ThrowIfDisposed();

            return this.Detach(this.client.Infer.TypeName(instanceType), ids);
        }

        public bool Detach(string typeName, params string[] ids)
        {
            this.ThrowIfDisposed();

            string indexName = this.Index;
            Func<IMetadataInfo, string, bool> func = (info, id) =>
                info.Id.Equals(id, StringComparison.InvariantCulture);

            var toInspect = this.GetCache(cond: info => info.TypeName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase)
                && info.IndexName.Equals(indexName, StringComparison.InvariantCultureIgnoreCase)
                ).ToList();

            IBulkRequest request = new BulkRequest();

            foreach (var id in ids)
            {
                var metadata = toInspect.SingleOrDefault(info => func.Invoke(info, id));
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
                    this.localCache.Remove(metadata);
                }
            }

            if (!request.Operations.Any())
                return true;

            IBulkResponse response = this.client.Bulk(request);
            if (response.ItemsWithErrors.Any())
                throw new BulkOperationException("There are problems when some instances were processed ",
                    response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));

            return true;
        }

        public bool Detach<TEntity>(params TEntity[] instances) where TEntity : class
        {
            this.ThrowIfDisposed();

            var typeName = this.client.Infer.TypeName<TEntity>();
            var toInspect = this.GetCache(cond: info =>
                info.TypeName.Equals(typeName, StringComparison.InvariantCulture)
                ).ToList();

            IBulkRequest request = new BulkRequest();

            foreach (var instance in instances)
            {
                var metadata = toInspect.FirstOrDefault(info => info.Instance == instance);
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
                    this.localCache.Remove(metadata);
                }
            }

            if (!request.Operations.Any())
                return true;

            IBulkResponse response = this.client.Bulk(request);
            if (response.ItemsWithErrors.Any())
                throw new BulkOperationException("There are problems when some instances were processed ",
                    response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));

            return true;
        }

        public bool Detach(Expression<Func<IMetadataInfo, bool>> exp)
        {
            this.ThrowIfDisposed();

            IBulkRequest request = new BulkRequest();
            var toRemove = this.GetCache(cond: exp.Compile())
                .ToList();

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
                this.localCache.Remove(metadata);
            }

            if (!request.Operations.Any())
                return true;

            IBulkResponse response = this.client.Bulk(request);
            if (response.ItemsWithErrors.Any())
                throw new BulkOperationException("There are problems when some instances were processed ",
                    response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));

            return true;
        }

        public void Clear(string indexName = null)
        {
            this.ThrowIfDisposed();

            IBulkRequest request = new BulkRequest();
            var toRemove = this.GetCache(indexName)
                .ToList();

            foreach (var metadata in toRemove)
            {
                this.localCache.Remove(metadata);
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
            
            IBulkResponse response = this.client.Bulk(request);
            if (response.ItemsWithErrors.Any())
                throw new BulkOperationException("There are problems when some instances were processed by clear operation.",
                    response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));
        }

        private void ClearAll()
        {
            this.ThrowIfDisposed();

            IBulkRequest request = new BulkRequest();
            var toRemove = this.localCache.Where(info => info.Origin == OriginContext.Newone)
                .ToList();

            foreach (var metadata in toRemove)
            {
                this.localCache.Remove(metadata);
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

            IBulkResponse response = this.client.Bulk(request);
            if (response.ItemsWithErrors.Any())
                throw new BulkOperationException("There are problems when some instances were processed by clear operation.",
                    response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));
        }

        /// <summary>
        /// Gets the cache.
        /// </summary>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="cond">The cond.</param>
        /// <returns></returns>
        private IEnumerable<IMetadataInfo> GetCache(string indexName = null, Func<IMetadataInfo, bool> cond = null)
        {
            return cond == null
                ? this.localCache.Where(info =>
                    (indexName ?? this.Index).Equals(info.IndexName, StringComparison.InvariantCulture)
                    )
                : this.localCache.Where(info =>
                    (indexName ?? this.Index).Equals(info.IndexName, StringComparison.InvariantCulture)
                    && cond.Invoke(info)
                    );
        }

        /// <summary>
        /// Flushes this instance.
        /// </summary>
        public void Flush()
        {
            this.ThrowIfDisposed();
            IBulkRequest request = new BulkRequest();

            var metadataToPersist = this.GetCache(cond: info => info.HasChanged()).ToList();
            foreach (var metadata in metadataToPersist)
            {
                request.Operations.Add(
                    new BulkUpdateOperation<object, object>(metadata.Id)
                    {
                        Index = metadata.IndexName,
                        Type = metadata.TypeName,
                        Version = metadata.Version,
                        Doc = metadata.Instance
                    }
                    );
            }

            var response = this.client.Bulk(request);
            if (response.Errors)
            {
                Exception innerException;
                try
                {
                    this.Restore(response.Items);
                    innerException = null;
                }
                catch (Exception ex)
                {
                    innerException = ex;
                }

                BulkOperationException exception = innerException == null
                    ? new BulkOperationException("There are problems when some instances were processed by clear operation.", response.ItemsWithErrors.Select(item => item.ToDocumentResponse()))
                    : new BulkOperationException("There are problems when some instances were processed by clear operation.", innerException, response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));

                throw exception;
            }
            
            // everything was gone well.. so It's requeried to update metadata with bulk response
            // so Version and Origin (for new instances).
        }

        /// <summary>
        /// Restores the specified instances.
        /// </summary>
        /// <param name="instancesToRestore">The instances to restore.</param>
        /// <exception cref="PersistentLayer.ElasticSearch.Exceptions.BulkOperationException">There are problems when some instances were processed by clear operation.</exception>
        private void Restore(IEnumerable<BulkOperationResponseItem> instancesToRestore)
        {
            // here It's requeried to restore instances which are persisted correctly.
            Func<IMetadataInfo, BulkOperationResponseItem, bool> func = (info, item) =>
                info.Id.Equals(item.Id, StringComparison.InvariantCulture)
                && info.TypeName.Equals(item.Type)
                && info.IndexName.Equals(item.Index);

            IBulkRequest request = new BulkRequest();

            foreach (var item in instancesToRestore.Where(item => item.IsValid))
            {
                var metadata = this.localCache.FirstOrDefault(info => func(info, item));
                if (metadata != null)
                {
                    //metadata.Restore(item.Version);
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
                        if (metadata.PreviousStatus.Instance != null)
                        {
                            request.Operations.Add(
                            new BulkUpdateOperation<object, object>(metadata.Id)
                            {
                                Index = metadata.IndexName,
                                Type = metadata.TypeName,
                                Version = metadata.Version,
                                Doc = metadata.PreviousStatus.Instance,
                                RetriesOnConflict = 2
                            });
                        }

                        //if (response.IsValid)
                        //    metadata.Restore(item.Version);
                    }
                }
            }

            var response = this.client.Bulk(request);
            if (response.Errors)
            {
                throw new BulkOperationException("There are problems when some instances were processed by clear operation.",
                response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.ThrowIfDisposed();

            this.ClearAll();
            this.Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this.disposed)
            {
                this.disposed = true;
            }
        }

        /// <summary>
        /// Throws if disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">The given cache was already disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name, "The given cache was already disposed.");
            }
        }
    }
}
