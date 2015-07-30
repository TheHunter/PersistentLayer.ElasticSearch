using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
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
        /// Converts into dynamic instance.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="dynamicProperties">The dynamic properties.</param>
        /// <returns></returns>
        public static dynamic AsDynamic(this object value, params KeyValuePair<string, object>[] dynamicProperties)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
                expando.Add(property.Name, property.GetValue(value));

            foreach (var dynamicProperty in dynamicProperties)
            {
                expando.Add(dynamicProperty.Key, dynamicProperty.Value);
            }

            return expando;
        }

        /// <summary>
        /// To the document response.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        public static DocOperationResponse ToDocumentResponse(this BulkOperationResponseItem document)
        {
            return new DocOperationResponse
            {
                Error = document.Error,
                Id = document.Id,
                Index = document.Index,
                Operation = document.Operation,
                Status = document.Status,
                Type = document.Type,
                Version = document.Version
            };
        }

        /// <summary>
        /// To the metadata.
        /// </summary>
        /// <typeparam name="TEntity">
        /// The type of the entity.
        /// </typeparam>
        /// <param name="response">
        /// The response.
        /// </param>
        /// <param name="evaluator">
        /// The evaluator.
        /// </param>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <param name="currentState">
        /// State of the current.
        /// </param>
        /// <param name="version">
        /// The version.
        /// </param>
        /// <param name="readOnly">
        /// The read Only.
        /// </param>
        /// <returns>
        /// </returns>
        public static MetadataWorker AsMetadata<TEntity>(this IGetResponse<TEntity> response, IObjectEvaluator evaluator, OriginContext origin, TEntity currentState = null, string version = null, bool readOnly = true)
            where TEntity : class
        {
            version = response.Version ?? version;
            return currentState == null ? new MetadataWorker(response.Id, response.Index, response.Type, response.Source, evaluator, origin, version)
                : new MetadataWorker(response.Id, response.Index, response.Type, response.Source, currentState, evaluator, origin, version);
        }

        /// <summary>
        /// Converts into metadata.
        /// </summary>
        /// <typeparam name="TEntity">
        /// The type of the entity.
        /// </typeparam>
        /// <param name="response">
        /// The response.
        /// </param>
        /// <param name="evaluator">
        /// The evaluator.
        /// </param>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <param name="currentState">
        /// State of the current.
        /// </param>
        /// <param name="version">
        /// The version.
        /// </param>
        /// <param name="readOnly">
        /// The read Only.
        /// </param>
        /// <returns>
        /// </returns>
        internal static MetadataWorker AsMetadata<TEntity>(this IHit<TEntity> response, IObjectEvaluator evaluator, OriginContext origin, TEntity currentState = null, string version = null, bool readOnly = true)
            where TEntity : class
        {
            version = response.Version ?? version;
            return currentState == null ? new MetadataWorker(response.Id, response.Index, response.Type, response.Source, evaluator, origin, version)
                : new MetadataWorker(response.Id, response.Index, response.Type, response.Source, currentState, evaluator, origin, version);
        }

        /// <summary>
        /// Ases the metadata.
        /// </summary>
        /// <typeparam name="TEntity">
        /// The type of the entity.
        /// </typeparam>
        /// <param name="response">
        /// The response.
        /// </param>
        /// <param name="evaluator">
        /// The evaluator.
        /// </param>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <param name="currentState">
        /// State of the current.
        /// </param>
        /// <param name="version">
        /// The version.
        /// </param>
        /// <param name="readOnly">
        /// The read Only.
        /// </param>
        /// <returns>
        /// </returns>
        internal static MetadataWorker AsMetadata<TEntity>(this IMultiGetHit<TEntity> response, IObjectEvaluator evaluator, OriginContext origin, TEntity currentState = null, string version = null, bool readOnly = true)
            where TEntity : class
        {
            version = response.Version ?? version;
            return currentState == null ? new MetadataWorker(response.Id, response.Index, response.Type, response.Source, evaluator, origin, version)
                : new MetadataWorker(response.Id, response.Index, response.Type, response.Source, currentState, evaluator, origin, version);
        }

        /// <summary>
        /// Ases the metadata.
        /// </summary>
        /// <typeparam name="TEntity">
        /// The type of the entity.
        /// </typeparam>
        /// <param name="response">
        /// The response.
        /// </param>
        /// <param name="evaluator">
        /// The evaluator.
        /// </param>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="currentState">
        /// State of the current.
        /// </param>
        /// <param name="version">
        /// The version.
        /// </param>
        /// <param name="readOnly">
        /// The read Only.
        /// </param>
        /// <returns>
        /// </returns>
        internal static MetadataWorker AsMetadata<TEntity>(this IIndexResponse response, IObjectEvaluator evaluator, OriginContext origin, TEntity instance, TEntity currentState = null, string version = null, bool readOnly = true)
            where TEntity : class
        {
            version = response.Version ?? version;
            return currentState == null ? new MetadataWorker(response.Id, response.Index, response.Type, instance, evaluator, origin, version)
                : new MetadataWorker(response.Id, response.Index, response.Type, instance, currentState, evaluator, origin, version);
        }
    }
}
