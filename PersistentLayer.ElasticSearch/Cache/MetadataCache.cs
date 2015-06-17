using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nest;
using PersistentLayer.ElasticSearch.Exceptions;
using PersistentLayer.ElasticSearch.Extensions;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Cache
{
    public class MetadataCache
        : IMetadataCache, IDisposable
    {
        private const string SessionFieldName = "$idsession";
        // private readonly IdResolver idResolver = new IdResolver();
        private readonly HashSet<IMetadataWorker> localCache;
        private readonly IElasticClient client;
        private readonly IEqualityComparer<IMetadataInfo> comparer;
        private bool disposed;

        public MetadataCache(string index, IElasticClient client)
        {
            if (string.IsNullOrWhiteSpace(index))
                throw new ArgumentException("The index name cannot be null or empty.", "index");

            if (client == null)
                throw new ArgumentNullException("client", "The elastic client cannot be null");

            this.Index = index;
            this.comparer = new MetadataComparer();
            this.localCache = new HashSet<IMetadataWorker>(this.comparer);
            this.client = client;
            this.disposed = false;
        }

        public string Index { get; private set; }

        public bool Cached<TEntity>(string index = null, params string[] ids) where TEntity : class
        {
            this.ThrowIfDisposed();

            Type instanceType = typeof(TEntity);
            return this.Cached(instanceType, index, ids);
        }

        public bool Cached(Type instanceType, string index = null, params string[] ids)
        {
            var typeName = this.client.Infer.IndexName(instanceType);
            return this.Cached(typeName, index, ids);
        }

        public bool Cached(string typeName, string index = null, params string[] ids)
        {
            this.ThrowIfDisposed();

            var toInspect = this.GetCache(index).ToList();
            return ids.All(s => toInspect.Any(info =>
                info.Id.Equals(s, StringComparison.InvariantCulture)
                && info.TypeName.Equals(typeName, StringComparison.InvariantCulture)));
        }

        public bool Cached(string index = null, params object[] instances)
        {
            this.ThrowIfDisposed();

            var inferrer = this.client.Infer;
            var toInspect = this.GetCache(index).ToList();
            return instances.All(instance => toInspect
                .Any(info => 
                    info.TypeName.Equals(inferrer.TypeName(instance.GetType()), StringComparison.InvariantCulture)
                    && info.Instance == instance));
        }

        /// <summary>
        /// Finds the first occurrence for the given parameters.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="DuplicatedInstanceException">if it founded more than one instance.</exception>
        public IMetadataWorker SingleOrDefault(string id, string typeName, string index = null)
        {
            var ind = index ?? this.Index;
            try
            {
                return this.localCache.SingleOrDefault(worker =>
                    worker.Id.Equals(id, StringComparison.InvariantCulture)
                    && worker.TypeName.Equals(typeName, StringComparison.InvariantCulture)
                    && worker.IndexName.Equals(ind, StringComparison.InvariantCulture)
                    );
            }
            catch (Exception ex)
            {
                throw new DuplicatedInstanceException(
                    string.Format("It was found more than one instance for the given parameter, Id:{0}, Typename: {1}, Index:{2}", id, typeName, ind),
                    ex);
            }
        }

        public IEnumerable<IMetadataWorker> FindMetadata(string typeName, string index = null)
        {
            return this.GetCache(index, worker => worker
                .TypeName.Equals(typeName, StringComparison.InvariantCulture));
        }

        public IEnumerable<IMetadataWorker> FindMetadata(string index = null, params object[] instances)
        {
            this.ThrowIfDisposed();

            var toInspect = this.GetCache(index).ToList();
            return instances.Select(instance => toInspect.FirstOrDefault(info => info.Instance == instance))
                .Where(info => info != null)
                .ToList();
        }

        public IEnumerable<IMetadataWorker> FindMetadata(Expression<Func<IMetadataWorker, bool>> exp, string index = null)
        {
            this.ThrowIfDisposed();
            return this.GetCache(indexName: index, cond: exp.Compile())
                .ToList();
        }

        public TResult MetadataExpression<TResult>(Expression<Func<IEnumerable<IMetadataWorker>, TResult>> expr, string index = null)
        {
            this.ThrowIfDisposed();
            return expr.Compile().Invoke(this.GetCache(index));
        }

        public bool Attach(params IMetadataWorker[] metadata)
        {
            this.ThrowIfDisposed();

            var ret = true;
            metadata.All(info =>
            {
                ret = ret && this.localCache.Add(info);
                return true;
            });
            return ret;
        }

        public bool AttachOrUpdate(params IMetadataWorker[] metadata)
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

        public bool Detach<TEntity>(string index = null, params string[] ids) where TEntity : class
        {
            this.ThrowIfDisposed();

            return this.Detach(this.client.Infer.TypeName<TEntity>(), index, ids);
        }

        public bool Detach(Type instanceType, string index = null, params string[] ids)
        {
            this.ThrowIfDisposed();

            return this.Detach(this.client.Infer.TypeName(instanceType), index, ids);
        }

        public bool Detach(string typeName, string index = null, params string[] ids)
        {
            this.ThrowIfDisposed();

            string indexName = index ?? this.Index;
            Func<IMetadataWorker, string, bool> func = (info, id) =>
                info.Id.Equals(id, StringComparison.InvariantCulture);

            var toInspect = this.GetCache(indexName, info => info.TypeName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase)
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

        public bool Detach<TEntity>(string index = null, params TEntity[] instances) where TEntity : class
        {
            this.ThrowIfDisposed();

            var typeName = this.client.Infer.TypeName<TEntity>();
            var toInspect = this.GetCache(indexName: index ?? this.Index, cond: info =>
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

        public bool Detach(Expression<Func<IMetadataWorker, bool>> exp)
        {
            this.ThrowIfDisposed();

            IBulkRequest request = new BulkRequest();
            var toRemove = this.GetCache(null, cond: exp.Compile())
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

            var toRemove = this.GetCache(indexName)
                .ToList();

            if (!toRemove.Any())
                return;

            IBulkRequest request = new BulkRequest();

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
        private IEnumerable<IMetadataWorker> GetCache(string indexName, Func<IMetadataWorker, bool> cond = null)
        {
            return this.localCache.Where(worker =>
                (indexName == null || worker.IndexName.Equals(indexName, StringComparison.InvariantCulture))
                && (cond == null || cond.Invoke(worker))
                );
        }

        /// <summary>
        /// Flushes this instance.
        /// </summary>
        public void Flush()
        {
            this.ThrowIfDisposed();
            IBulkRequest request = new BulkRequest();
            request.Operations = new List<IBulkOperation>();

            var metadataToPersist = this.GetCache(null, info => info.Origin == OriginContext.Newone || info.HasChanged())
                .ToList();

            if (!metadataToPersist.Any())
                return;

            foreach (var metadata in metadataToPersist)
            {
                request.Operations.Add(
                    new BulkUpdateOperation<object, object>(metadata.Id)
                    {
                        Index = metadata.IndexName,
                        Type = metadata.TypeName,
                        Version = metadata.Version,
                        Doc = metadata.Instance,
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
            foreach (var item in response.Items)
            {
                var metadata = this.SingleOrDefault(item.Id, item.Type, this.Index);
                if (metadata == null)
                    continue;

                metadata.BecomePersistent(item.Version);

                #region
                
                if (metadata.Origin == OriginContext.Newone)
                {
                    BulkOperationResponseItem current = item;
                    var respo = this.client.Update<object>(descriptor => descriptor
                        .Id(current.Id)
                        .Type(current.Type)
                        .Index(this.Index)
                        .Version(Convert.ToInt64(current.Version))
                        .Script(string.Format("ctx._source.remove(\"{0}\")", "\\" + SessionFieldName))
                        );

                    if (respo.IsValid)
                    {
                        metadata.BecomePersistent(respo.Version);
                    }
                    else
                    {
                        // It's needed to write this error into a log file...
                    }
                }
                else
                {
                    metadata.BecomePersistent(item.Version);
                }
                
                #endregion
            }
        }

        /// <summary>
        /// Restores the specified instances.
        /// </summary>
        /// <param name="instancesToRestore">The instances to restore.</param>
        /// <exception cref="PersistentLayer.ElasticSearch.Exceptions.BulkOperationException">There are problems when some instances were processed by clear operation.</exception>
        private void Restore(IEnumerable<BulkOperationResponseItem> instancesToRestore)
        {
            // here It's requeried to restore instances which are persisted correctly.
            Func<IMetadataWorker, BulkOperationResponseItem, bool> func = (info, item) =>
                info.Id.Equals(item.Id, StringComparison.InvariantCulture)
                && info.TypeName.Equals(item.Type)
                && info.IndexName.Equals(item.Index);

            IBulkRequest request = new BulkRequest();

            foreach (var item in instancesToRestore.Where(item => item.IsValid))
            {
                var metadata = this.localCache.FirstOrDefault(info => func(info, item));
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

                        // if (response.IsValid)
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

            this.Clear();
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
