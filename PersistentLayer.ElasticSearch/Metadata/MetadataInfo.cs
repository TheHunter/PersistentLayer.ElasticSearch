using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// 
    /// </summary>
    public class MetadataInfo
        : IMetadataInfo
    {
        private readonly Func<object, string> serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataInfo"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="version">The version.</param>
        public MetadataInfo(string id, object instance, Func<object, string> serializer, OriginContext origin, string version = null)
        {
            this.serializer = serializer;

            this.Id = id;
            this.CurrentStatus = instance;
            this.PreviousStatus = serializer.Invoke(instance);
            this.Origin = origin;
            this.InstanceType = instance.GetType();
            this.Version = version == null || version.Trim().Equals(string.Empty) ? "0": version;
        }

        /// <summary>
        /// Gets the identifier of current instance.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; private set; }

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
        public string PreviousStatus { get; private set; }

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
            return !currentStatus.Equals(this.PreviousStatus, StringComparison.CurrentCulture);
        }
    }
}
