using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Cache
{
    /// <summary>
    /// Rappresents the instance status with basic metadata.
    /// </summary>
    public interface IMetadata
    {
        /// <summary>
        /// Gets the identifier of current instance.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        string Id { get; }

        /// <summary>
        /// Gets the name of the index.
        /// </summary>
        /// <value>
        /// The name of the index.
        /// </value>
        string IndexName { get; }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <value>
        /// The name of the type.
        /// </value>
        string TypeName { get; }

        /// <summary>
        /// Gets the current status.
        /// </summary>
        /// <value>
        /// The current status.
        /// </value>
        object Instance { get; }
        
        /// <summary>
        /// Gets the type of the instance.
        /// </summary>
        /// <value>
        /// The type of the instance.
        /// </value>
        Type InstanceType { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        string Version { get; }
    }
}
