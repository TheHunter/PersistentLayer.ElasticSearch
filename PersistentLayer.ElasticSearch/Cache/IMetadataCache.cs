using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Cache
{
    public interface IMetadataCache
    {
        string Index { get; }

        bool Cached<TEntity>(params string[] id)
            where TEntity : class;

        bool Cached(params object[] instances);

        IEnumerable<IMetadataInfo> FindMetadata(params object[] instances);

        IEnumerable<IMetadataInfo> FindMetadata(Expression<Func<IMetadataInfo, bool>> exp);

        TResult MetadataExpression<TResult>(Expression<Func<IEnumerable<IMetadataInfo>, TResult>> expr);

        bool Attach(params IMetadataInfo[] metadata);

        bool Detach<TEntity>(params string[] id)
            where TEntity: class;

        bool Detach(Type instanceType, params string[] id);

        bool Detach<TEntity>(params TEntity[] instances)
            where TEntity : class;

        bool Detach(Expression<Func<IMetadataInfo, bool>> exp);

        void Clear();
    }
}
