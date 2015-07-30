using System;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Rappresents a document map builder for a particolar document type.
    /// </summary>
    public interface IDocumentMapBuilder
    {
        /// <summary>
        /// Gets the type of the documen.
        /// </summary>
        /// <value>
        /// The type of the documen.
        /// </value>
        Type DocumentType { get; }

        /// <summary>
        /// Builds the specified key gen type.
        /// </summary>
        /// <param name="keyGenType">Type of the key gen.</param>
        /// <returns></returns>
        IDocumentMapper Build(KeyGenType keyGenType = KeyGenType.Identity);
    }
}
