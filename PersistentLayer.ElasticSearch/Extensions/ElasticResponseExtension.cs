using System;
using Nest;
using PersistentLayer.ElasticSearch.Mapping;

namespace PersistentLayer.ElasticSearch.Extensions
{
    /// <summary>
    /// Extension methods for IResponses instances.
    /// </summary>
    public static class ElasticResponseExtension
    {
        /// <summary>
        /// Ovverrides the properties.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="document">The document.</param>
        /// <exception cref="ArgumentNullException">
        /// response;response for the given document must be referenced.
        /// or
        /// mapper;Mapper for the given document must be referenced.
        /// </exception>
        public static void OverrideProperties(this IIndexResponse response, IDocumentMapper mapper, object document)
        {
            if (response == null)
                throw new ArgumentNullException("response", "response for the given document must be referenced.");

            if (mapper == null)
                throw new ArgumentNullException("mapper", "Mapper for the given document must be referenced.");

            document.SetPropertyValue(response.Id, mapper.Id);
            document.SetPropertyValue(response.Version, mapper.Version);
        }

        /// <summary>
        /// Ovverrides the properties.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="document">The document.</param>
        /// <exception cref="ArgumentNullException">
        /// response;response for the given document must be referenced.
        /// or
        /// mapper;Mapper for the given document must be referenced.
        /// </exception>
        public static void OverrideProperties(this IUpdateResponse response, IDocumentMapper mapper, object document)
        {
            if (response == null)
                throw new ArgumentNullException("response", "response for the given document must be referenced.");

            if (mapper == null)
                throw new ArgumentNullException("mapper", "Mapper for the given document must be referenced.");

            document.SetPropertyValue(response.Id, mapper.Id);
            document.SetPropertyValue(response.Version, mapper.Version);
        }

        /// <summary>
        /// Overrides the properties.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="response">The response.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="document">The document.</param>
        /// <exception cref="ArgumentNullException">
        /// response;response for the given document must be referenced.
        /// or
        /// mapper;Mapper for the given document must be referenced.
        /// </exception>
        public static void OverrideProperties<TDocument>(this IGetResponse<TDocument> response, IDocumentMapper mapper, object document)
            where TDocument : class
        {
            if (response == null)
                throw new ArgumentNullException("response", "response for the given document must be referenced.");

            if (mapper == null)
                throw new ArgumentNullException("mapper", "Mapper for the given document must be referenced.");

            if (document.GetPropertyValue(mapper.Id) == null)
                document.SetPropertyValue(response.Id, mapper.Id);

            document.SetPropertyValue(response.Version, mapper.Version);
        }

        public static void OverrideProperties<TDocument>(this IHit<TDocument> response, IDocumentMapper mapper, object document)
            where TDocument : class
        {
            if (response == null)
                throw new ArgumentNullException("response", "response for the given document must be referenced.");

            if (mapper == null)
                throw new ArgumentNullException("mapper", "Mapper for the given document must be referenced.");

            if (document.GetPropertyValue(mapper.Id) == null)
                document.SetPropertyValue(response.Id, mapper.Id);

            document.SetPropertyValue(response.Version, mapper.Version);
        }

        public static void OverrideProperties(this BulkOperationResponseItem response, IDocumentMapper mapper, object document)
        {
            if (response == null)
                throw new ArgumentNullException("response", "response for the given document must be referenced.");

            if (mapper == null)
                throw new ArgumentNullException("mapper", "Mapper for the given document must be referenced.");

            if (document.GetPropertyValue(mapper.Id) == null)
                document.SetPropertyValue(response.Id, mapper.Id);

            document.SetPropertyValue(response.Version, mapper.Version);
        }
    }
}
