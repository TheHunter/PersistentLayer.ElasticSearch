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
        private readonly ElasticClient client;
        private bool disposed = false;

        public MetadataCache(string index, ElasticClient client)
        {
            this.Index = index;
            this.localCache = new HashSet<IMetadataInfo>(new MetadataComparer());
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

            return ids.All(s => this.localCache.Any(info => info.Id.Equals(s, StringComparison.InvariantCulture)
                && info.InstanceType == instanceType));
        }

        public bool Cached(params object[] instances)
        {
            this.ThrowIfDisposed();

            return instances.All(instance => this.localCache.Any(info => info.CurrentStatus == instance));
        }

        public IEnumerable<IMetadataInfo> FindMetadata(params object[] instances)
        {
            this.ThrowIfDisposed();

            return this.localCache.Where(info => instances.Any(o => o == info.CurrentStatus));
        }

        public IEnumerable<IMetadataInfo> FindMetadata(Expression<Func<IMetadataInfo, bool>> exp)
        {
            this.ThrowIfDisposed();

            return this.localCache.Where(exp.Compile());
        }

        public TResult MetadataExpression<TResult>(Expression<Func<IEnumerable<IMetadataInfo>, TResult>> expr)
        {
            this.ThrowIfDisposed();

            return expr.Compile().Invoke(this.localCache);
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

            if (request.Operations.Any())
            {
                IBulkResponse response = this.client.Bulk(request);
                if (response.ItemsWithErrors.Any())
                {
                    throw new BulkOperationException("There are problems when some instances were processed ", response.ItemsWithErrors.Select(item => item.ToDocumentResponse()));
                }
            }

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

            throw new NotImplementedException();
        }

        public void Clear()
        {
            this.ThrowIfDisposed();
            // cancello le righe che hanno una origine NEW
            // quindi devono essre cancellate dallo storage.

            

            this.localCache.Clear();
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
