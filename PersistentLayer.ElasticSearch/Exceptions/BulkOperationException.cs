using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PersistentLayer.ElasticSearch.Metadata;

namespace PersistentLayer.ElasticSearch.Exceptions
{
    /// <summary>
    /// Rappresents all documents which have caused an error when 
    /// </summary>
    public class BulkOperationException
        : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkOperationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="documentResponses">The document responses.</param>
        public BulkOperationException(string message, IEnumerable<DocOperationResponse> documentResponses)
            : base(message)
        {
            this.DocumentResponses = new List<DocOperationResponse>(documentResponses);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkOperationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="documentResponses">The document responses.</param>
        public BulkOperationException(string message, Exception innerException, IEnumerable<DocOperationResponse> documentResponses)
            : base(message, innerException)
        {
            this.DocumentResponses = new List<DocOperationResponse>(documentResponses);
        }


        public IEnumerable<DocOperationResponse> DocumentResponses { get; private set; }
    }
}
