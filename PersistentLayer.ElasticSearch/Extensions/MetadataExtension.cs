using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nest;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Extensions
{
    /// <summary>
    /// Extensions for making metadata from elastic responses.
    /// </summary>
    public static class MetadataExtension
    {
        /// <summary>
        /// To the metadata.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="response">The response.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="currentState">State of the current.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        public static MetadataInfo AsMetadata<TEntity>(this IGetResponse<TEntity> response, Func<object, string> serializer, OriginContext origin, TEntity currentState = null, string version = null)
            where TEntity : class
        {
            version = response.Version ?? version;
            return currentState == null ? new MetadataInfo(response.Id, response.Index, response.Type, response.Source, serializer, origin, version)
                : new MetadataInfo(response.Id, response.Index, response.Type, response.Source, currentState, serializer, origin, version);
        }

        /// <summary>
        /// Ases the metadata.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="response">The response.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="currentState">State of the current.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        internal static MetadataInfo AsMetadata<TEntity>(this IHit<TEntity> response, Func<object, string> serializer, OriginContext origin, TEntity currentState = null, string version = null)
            where TEntity : class
        {
            version = response.Version ?? version;
            return currentState == null ? new MetadataInfo(response.Id, response.Index, response.Type, response.Source, serializer, origin, version)
                : new MetadataInfo(response.Id, response.Index, response.Type, response.Source, currentState, serializer, origin, version);
        }

        /// <summary>
        /// Ases the metadata.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="response">The response.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="currentState">State of the current.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        internal static MetadataInfo AsMetadata<TEntity>(this IMultiGetHit<TEntity> response, Func<object, string> serializer, OriginContext origin, TEntity currentState = null, string version = null)
            where TEntity : class
        {
            version = response.Version ?? version;
            return currentState == null ? new MetadataInfo(response.Id, response.Index, response.Type, response.Source, serializer, origin, version)
                : new MetadataInfo(response.Id, response.Index, response.Type, response.Source, currentState, serializer, origin, version);
        }

        /// <summary>
        /// Ases the metadata.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="response">The response.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="currentState">State of the current.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        internal static MetadataInfo AsMetadata<TEntity>(this IIndexResponse response, Func<object, string> serializer, OriginContext origin, TEntity instance, TEntity currentState = null, string version = null)
            where TEntity : class
        {
            version = response.Version ?? version;
            return currentState == null ? new MetadataInfo(response.Id, response.Index, response.Type, instance, serializer, origin, version)
                : new MetadataInfo(response.Id, response.Index, response.Type, instance, currentState, serializer, origin, version);
        }
    }
}
