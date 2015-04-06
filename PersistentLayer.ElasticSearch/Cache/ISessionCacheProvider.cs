using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Cache
{
    public interface ISessionCacheProvider
    {
        string Index { get; }

        bool Cached(Expression<Func<IMetadataInfo, bool>> exp);

        bool Attach(params IMetadataInfo[] metadata);

        bool Detach<TEntity>(params object[] id)
            where TEntity: class;

        bool Detach(Expression<Func<IMetadataInfo, bool>> exp);
    }
}
