using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Indicates the key generation strategy for documents.
    /// </summary>
    public enum KeyGenType
    {
        /// <summary>
        /// The native strategy generation.
        /// </summary>
        Native,

        /// <summary>
        /// The identity strategy.
        /// </summary>
        Identity,

        /// <summary>
        /// The assigned strategy.
        /// </summary>
        Assigned
    }
}
