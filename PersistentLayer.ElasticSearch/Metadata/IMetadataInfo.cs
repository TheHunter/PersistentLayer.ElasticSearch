using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Rappresents the metadata about an instance present into session cache.
    /// </summary>
    public interface IMetadataInfo
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
        object CurrentStatus { get; }

        /// <summary>
        /// Gets the previous status rappresented in json format.
        /// </summary>
        /// <value>
        /// The previous status.
        /// </value>
        string OriginalStatus { get; }

        /// <summary>
        /// Gets the origin of the current instance.
        /// </summary>
        /// <value>
        /// The origin.
        /// </value>
        OriginContext Origin { get; }

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

        /// <summary>
        /// Determines whether this instance has changed.
        /// </summary>
        /// <returns></returns>
        bool HasChanged();

        /// <summary>
        /// Updates the specified metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        void Update(IMetadataInfo metadata);

        /// <summary>
        /// Becomes the persistent the internal metadata, updating the current version.
        /// </summary>
        /// <param name="version">The version to update</param>
        void BecomePersistent(string version);
    }
}
