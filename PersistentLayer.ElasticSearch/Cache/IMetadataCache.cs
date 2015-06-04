using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Cache
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetadataCache
    {
        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        string Index { get; }

        /// <summary>
        /// Cacheds the specified ids.
        /// </summary>
        /// <typeparam name="TEntity">
        /// The type of the entity.
        /// </typeparam>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <param name="ids">
        /// The ids.
        /// </param>
        /// <returns>
        /// </returns>
        bool Cached<TEntity>(string index = null, params string[] ids)
            where TEntity : class;

        /// <summary>
        /// Cacheds the specified instance type.
        /// </summary>
        /// <param name="instanceType">KeyGenType of the instance.</param>
        /// <param name="index">Index name which documents will be saved</param>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Cached(Type instanceType, string index = null, params string[] ids);

        /// <summary>
        /// Cacheds the specified type name.
        /// </summary>
        /// <param name="typeName">
        /// Name of the type.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <param name="ids">
        /// The ids.
        /// </param>
        /// <returns>
        /// </returns>
        bool Cached(string typeName, string index = null, params string[] ids);

        /// <summary>
        /// Cacheds the specified instances.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        bool Cached(string index = null, params object[] instances);

        /// <summary>
        /// Finds the first occurrence for the given parameters.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        IMetadataWorker SingleOrDefault(string id, string typeName, string index = null);

        /// <summary>
        /// Finds the metadata.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        IEnumerable<IMetadataWorker> FindMetadata(string typeName, string index = null);

        /// <summary>
        /// Finds the metadata.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        IEnumerable<IMetadataWorker> FindMetadata(string index = null, params object[] instances);

        /// <summary>
        /// Finds the metadata.
        /// </summary>
        /// <param name="exp">
        /// The exp.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// </returns>
        IEnumerable<IMetadataWorker> FindMetadata(Expression<Func<IMetadataWorker, bool>> exp, string index = null);

        /// <summary>
        /// Metadatas the expression.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result.
        /// </typeparam>
        /// <param name="expr">
        /// The expr.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// </returns>
        TResult MetadataExpression<TResult>(Expression<Func<IEnumerable<IMetadataWorker>, TResult>> expr, string index = null);

        /// <summary>
        /// Attaches the specified metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <returns></returns>
        bool Attach(params IMetadataWorker[] metadata);

        /// <summary>
        /// Attaches the or update.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <returns></returns>
        bool AttachOrUpdate(params IMetadataWorker[] metadata);

        /// <summary>
        /// Detaches the specified ids.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Detach<TEntity>(string index = null, params string[] ids)
            where TEntity: class;

        /// <summary>
        /// Detaches the specified instance type.
        /// </summary>
        /// <param name="instanceType">KeyGenType of the instance.</param>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Detach(Type instanceType, string index = null, params string[] ids);

        /// <summary>
        /// Detaches the specified type name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        bool Detach(string typeName, string index = null, params string[] ids);

        /// <summary>
        /// Detaches the specified instances.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        bool Detach<TEntity>(string index = null, params TEntity[] instances)
            where TEntity : class;

        /// <summary>
        /// Detaches the specified exp.
        /// </summary>
        /// <param name="exp">The exp.</param>
        /// <returns></returns>
        bool Detach(Expression<Func<IMetadataWorker, bool>> exp);

        /// <summary>
        /// Clears the specified index name.
        /// </summary>
        /// <param name="indexName">Name of the index.</param>
        void Clear(string indexName = null);
    }
}
