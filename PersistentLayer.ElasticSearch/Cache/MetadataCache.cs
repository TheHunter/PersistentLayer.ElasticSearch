using System;
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
        private bool disposed = false;

        public MetadataCache(string index, IElasticClient client)
        {
            this.Index = index;
            this.comparer = new MetadataComparer();
            this.localCache = new HashSet<IMetadataInfo>(this.comparer);
            this.client = client;
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
            this.ThrowIfDisposed();

            var typeName = this.client.Infer.IndexName(instanceType);
            return ids.All(s => this.localCache.Any(info =>
                info.Id.Equals(s, StringComparison.InvariantCulture)
                && info.TypeName.Equals(typeName, StringComparison.InvariantCulture)));
        }

        public bool Cached(params object[] instances)
        {
            this.ThrowIfDisposed();

            return instances.All(instance => this.localCache.Any(info =>
                this.Index.Equals(info.IndexName, StringComparison.InvariantCulture)
                && info.CurrentStatus == instance));
        }

        public IEnumerable<IMetadataInfo> FindMetadata(params object[] instances)
        {
            this.ThrowIfDisposed();

            return this.localCache.Where(info => instances.Any(instance => 
                this.Index.Equals(info.IndexName, StringComparison.InvariantCulture)
                && info.CurrentStatus == instance));
        }

        public IEnumerable<IMetadataInfo> FindMetadata(Expression<Func<IMetadataInfo, bool>> exp)
        {
            this.ThrowIfDisposed();
            return this.GetCache()
                .Where(info => exp.Compile().Invoke(info));
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
                info.Id.Equals(id, StringComparison.InvariantCulture)
                && info.TypeName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase)
                && info.IndexName.Equals(indexName, StringComparison.InvariantCultureIgnoreCase);

            IBulkRequest request = new BulkRequest();

            foreach (var id in ids)
            {
                var metadata = this.localCache.SingleOrDefault(info => func.Invoke(info, id));
                if (metadata != null)
                {
                    if (metadata.Origin == OriginContext.Newone)
                    {
                        request.Operations.Add(
                            new BulkDeleteOperation<object>(id)
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

            throw new NotImplementedException();
        }

        public bool Detach(Expression<Func<IMetadataInfo, bool>> exp)
        {
            this.ThrowIfDisposed();

            IBulkRequest request = new BulkRequest();
            var toRemove = this.GetCache().Where(exp.Compile())
                .ToList();

            foreach (var metadataInfo in toRemove)
            {
                
            }

            return true;
        }

        public void Clear()
        {
            this.ThrowIfDisposed();

            Func<IMetadataInfo, bool> exp = info =>
                info.Origin == OriginContext.Newone
                && info.IndexName.Equals(this.Index, StringComparison.InvariantCultureIgnoreCase);

            IBulkRequest request = new BulkRequest();
            var toRemove = this.localCache.Where(exp)
                .ToList();

            foreach (var metadata in toRemove)
            {
                this.localCache.Remove(metadata);
                request.Operations.Add(
                            new BulkDeleteOperation<object>(metadata.Id)
                            {
                                Index = metadata.IndexName,
                                Type = metadata.TypeName,
                                Version = metadata.Version
                            });
            }
            // I make a bulk delete for decrease response latency.
            this.client.Bulk(request);
            this.localCache.Clear();
        }

        private IEnumerable<IMetadataInfo> GetCache(string indexName = null)
        {
            return
                this.localCache.Where(info => 
                    (indexName ?? this.Index).Equals(info.IndexName, StringComparison.InvariantCulture));
        }

        public void Dispose()
        {
            this.ThrowIfDisposed();

            this.Clear();
            this.disposed = true;
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
