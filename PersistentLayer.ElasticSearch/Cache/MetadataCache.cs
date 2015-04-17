using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Cache
{
    public class MetadataCache
        : IMetadataCache, IDisposable
    {
        private readonly HashSet<IMetadataInfo> localCache;
        private readonly Dictionary<Type, List<IMetadataInfo>> metadataToDelete;

        public MetadataCache(string index)
        {
            this.Index = index;
            this.localCache = new HashSet<IMetadataInfo>();
            this.metadataToDelete = new Dictionary<Type, List<IMetadataInfo>>();
        }

        public string Index { get; private set; }

        public bool Cached<TEntity>(params string[] ids) where TEntity : class
        {
            Type instanceType = typeof(TEntity);
            return this.Cached(instanceType, ids);
        }

        public bool Cached(Type instanceType, params string[] ids)
        {
            return ids.All(s => this.localCache.Any(info => info.Id.Equals(s, StringComparison.InvariantCulture) && info.InstanceType == instanceType));
        }

        public bool Cached(params object[] instances)
        {
            return instances.All(instance => this.localCache.Any(info => info.CurrentStatus == instance));
        }

        public IEnumerable<IMetadataInfo> FindMetadata(params object[] instances)
        {
            return this.localCache.Where(info => instances.Any(o => o == info.CurrentStatus));
        }

        public IEnumerable<IMetadataInfo> FindMetadata(Expression<Func<IMetadataInfo, bool>> exp)
        {
            return this.localCache.Where(exp.Compile());
        }

        public TResult MetadataExpression<TResult>(Expression<Func<IEnumerable<IMetadataInfo>, TResult>> expr)
        {
            return expr.Compile().Invoke(this.localCache);
        }

        public bool Attach(params IMetadataInfo[] metadata)
        {
            bool ret = true;
            metadata.All(info =>
            {
                ret = ret && this.localCache.Add(info);
                return true;
            });
            return ret;
        }

        public bool Detach<TEntity>(params string[] id) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Detach(Type instanceType, params string[] id)
        {
            throw new NotImplementedException();
        }

        public bool Detach<TEntity>(params TEntity[] instances) where TEntity : class
        {
            throw new NotImplementedException();
        }

        public bool Detach(Expression<Func<IMetadataInfo, bool>> exp)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            this.localCache.Clear();
        }

        public void Dispose()
        {
            this.Clear();
        }
    }
}
