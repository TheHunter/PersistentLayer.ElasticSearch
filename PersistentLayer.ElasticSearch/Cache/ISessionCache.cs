using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Cache
{
    /// <summary>
    /// Rappresents a local cache for ISession instances.
    /// </summary>
    public interface ISessionCache
        : IDisposable
    {
        /// <summary>
        /// Gets the index related to this cache.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        string Index { get; }

        /// <summary>
        /// Indicates if documents with the given identifiers are present into this cache.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Cached<TEntity>(params string[] ids) where TEntity : class;

        /// <summary>
        /// Indicates if documents with the given identifiers are present into this cache.
        /// </summary>
        /// <param name="instanceType">Type of the instance.</param>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Cached(Type instanceType, params string[] ids);

        /// <summary>
        /// Indicates if documents with the given identifiers are present into this cache.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Cached(string typeName, params string[] ids);

        /// <summary>
        /// Indicates if documents indicated are cached.
        /// </summary>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        bool Cached(params object[] instances);

        /// <summary>
        /// Find one or null reference about metadata with the given parameters.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        IMetadataWorker SingleOrDefault(string id, string typeName);

        /// <summary>
        /// Finds the metadata related to the given type name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        IEnumerable<IMetadataWorker> FindMetadata(string typeName);

        /// <summary>
        /// Finds the metadata related to the given documents.
        /// </summary>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        IEnumerable<IMetadataWorker> FindMetadata(params object[] instances);

        /// <summary>
        /// Gets all metadata present into this cache.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        IEnumerable<IMetadataWorker> Metadata { get; }

        /// <summary>
        /// Attaches the specified metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <returns></returns>
        bool Attach(params IMetadataWorker[] metadata);

        /// <summary>
        /// Attaches or updates the given metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <returns></returns>
        bool AttachOrUpdate(params IMetadataWorker[] metadata);

        /// <summary>
        /// Detaches documents related to specified ids.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Detach<TEntity>(params string[] ids) where TEntity : class;

        /// <summary>
        /// Detaches the specified instance type.
        /// </summary>
        /// <param name="instanceType">Type of the instance.</param>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Detach(Type instanceType, params string[] ids);

        /// <summary>
        /// Detaches documents with the specified ids and type name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Detach(string typeName, params string[] ids);

        /// <summary>
        /// Detaches the specified instances.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        bool Detach<TEntity>(params TEntity[] instances) where TEntity : class;

        /// <summary>
        /// Detaches documents with the specified expression.
        /// </summary>
        /// <param name="exp">The exp.</param>
        /// <returns></returns>
        bool Detach(Expression<Func<IMetadataWorker, bool>> exp);

        /// <summary>
        /// Clears this instance removing all metadata..
        /// </summary>
        void Clear();
    }
}
