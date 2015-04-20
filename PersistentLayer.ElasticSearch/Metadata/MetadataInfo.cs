using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Metadata
    /// </summary>
    public class MetadataInfo
        : IMetadataInfo
    {
        private readonly Func<object, string> serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataInfo"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="version">The version.</param>
        public MetadataInfo(string id, string indexName, string typeName,
            object instance, Func<object, string> serializer, OriginContext origin, string version = null)
        {
            this.serializer = serializer;

            this.Id = id;
            this.IndexName = indexName;
            this.TypeName = typeName;
            this.CurrentStatus = instance;
            this.OriginalStatus = serializer.Invoke(instance);
            this.Origin = origin;
            this.InstanceType = instance.GetType();
            this.Version = version == null || version.Trim().Equals(string.Empty) ? "0" : version;
        }

        /// <summary>
        /// Gets the identifier of current instance.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the name of the index.
        /// </summary>
        /// <value>
        /// The name of the index.
        /// </value>
        public string IndexName { get; private set; }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <value>
        /// The name of the type.
        /// </value>
        public string TypeName { get; private set; }

        /// <summary>
        /// Gets the current status.
        /// </summary>
        /// <value>
        /// The current status.
        /// </value>
        public object CurrentStatus { get; private set; }

        /// <summary>
        /// Gets the previous status rappresented in json format.
        /// </summary>
        /// <value>
        /// The previous status.
        /// </value>
        public string OriginalStatus { get; private set; }

        /// <summary>
        /// Gets the origin of the current instance.
        /// </summary>
        /// <value>
        /// The origin.
        /// </value>
        public OriginContext Origin { get; private set; }

        /// <summary>
        /// Gets the type of the instance.
        /// </summary>
        /// <value>
        /// The type of the instance.
        /// </value>
        public Type InstanceType { get; private set; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public string Version { get; private set; }

        /// <summary>
        /// Determines whether this instance has changed.
        /// </summary>
        /// <returns></returns>
        public bool HasChanged()
        {
            string currentStatus = this.serializer.Invoke(this.CurrentStatus);
            return !currentStatus.Equals(this.OriginalStatus, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Updates the status.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="version">The version.</param>
        /// <exception cref="System.ArgumentException">The given instance cannot be replace to actual statement becuase its type is not compatible.;instance</exception>
        public void UpdateStatus(object instance, string version)
        {
            if (!this.InstanceType.IsInstanceOfType(instance))
                throw new ArgumentException("The given instance cannot be replace to actual statement becuase its type is not compatible.", "instance");

            this.OriginalStatus = this.serializer.Invoke(this.CurrentStatus);
            this.CurrentStatus = instance; //using a merge It could be better...
            this.InstanceType = instance.GetType();
            this.Version = version;
        }
    }
}
