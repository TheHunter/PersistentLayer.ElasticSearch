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
    public class SessionCacheImpl
        : ISessionCache
    {
        private readonly HashSet<IMetadataWorker> localCache;
        private readonly IElasticClient client;
        private readonly IEqualityComparer<IMetadataInfo> comparer;
        private bool disposed;

        public SessionCacheImpl(string index, IElasticClient client)
        {
            if (string.IsNullOrWhiteSpace(index))
                throw new ArgumentException("The index name cannot be null or empty.", "index");

            if (client == null)
                throw new ArgumentNullException("client", "The elastic client cannot be null");

            this.Index = index;
            this.comparer = new IndexMetadataComparer();
            this.localCache = new HashSet<IMetadataWorker>(this.comparer);
            this.client = client;
            this.disposed = false;
        }

        public string Index { get; private set; }

        public bool Cached<TEntity>(params string[] ids) where TEntity : class
        {
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

        public IMetadataWorker SingleOrDefault(string id, string typeName)
        {
            try
            {
                return this.localCache.SingleOrDefault(worker =>
                    worker.Id.Equals(id, StringComparison.InvariantCulture)
                    && worker.TypeName.Equals(typeName, StringComparison.InvariantCulture)
                    );
            }
            catch (Exception ex)
            {
                throw new DuplicatedInstanceException(
                    string.Format("It was found more than one instance for the given parameter, Id:{0}, Typename: {1}, Index:{2}", id, typeName, this.Index),
                    ex);
            }
        }

        public IEnumerable<IMetadataWorker> FindMetadata(string typeName)
        {
            return this.GetCache(typeName);
        }

        public IEnumerable<IMetadataWorker> FindMetadata(params object[] instances)
        {
            var toInspect = this.GetCache().ToList();
            return instances.Select(instance => toInspect.FirstOrDefault(info => info.Instance == instance))
                .Where(info => info != null)
                .ToList();
        }

        public IEnumerable<IMetadataWorker> Metadata
        {
            get { return this.localCache; }
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

        public bool Detach<TEntity>(params string[] ids) where TEntity : class
        {
            return this.Detach(this.client.Infer.TypeName<TEntity>(), ids);
        }

        public bool Detach(Type instanceType, params string[] ids)
        {
            return this.Detach(this.client.Infer.TypeName(instanceType), ids);
        }

        public bool Detach(string typeName, params string[] ids)
        {
            this.ThrowIfDisposed();

            Func<IMetadataWorker, string, bool> func = (info, id) =>
                info.Id.Equals(id, StringComparison.InvariantCulture);

            var toInspect = this.GetCache(typeName).ToList();

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
            var toInspect = this.GetCache(cond: info => info.TypeName.Equals(typeName, StringComparison.InvariantCulture)
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

            if (request.Operations == null || !request.Operations.Any())
                return true;

            IBulkResponse response = this.client.Bulk(request);
            if (response.ItemsWithErrors.Any())
                throw new BulkOperationException("There are problems when some instances were processed ",
                    response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));

            return true;
        }

        public bool Detach(Expression<Func<IMetadataWorker, bool>> exp)
        {
            var toRemove = this.GetCache(cond: exp.Compile())
                .ToList();

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

        public void Clear()
        {
            this.localCache.Clear();
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

        private IEnumerable<IMetadataWorker> GetCache(string typeName = null, Func<IMetadataWorker, bool> cond = null)
        {
            return this.localCache.Where(worker =>
                (cond == null || cond.Invoke(worker))
                && (typeName == null || typeName.Equals(worker.TypeName, StringComparison.InvariantCultureIgnoreCase))
                );
        }
    }
}
