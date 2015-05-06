using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nest;
using Nest.Resolvers;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Metadata for storing entity info.
    /// </summary>
    public class MetadataInfo
        : IMetadataInfo
    {
        private Func<object, string> serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataInfo"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="currentStatus">The current status.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="version">The version.</param>
        public MetadataInfo(string id, string indexName, string typeName,
            object currentStatus, Func<object, string> serializer, OriginContext origin, string version)
        {
            this.OnInit(id, indexName, typeName, currentStatus, currentStatus, serializer, origin, version);
        }

        public MetadataInfo(string id, string indexName, string typeName,
            object originalStatus, object currentStatus, Func<object, string> serializer, OriginContext origin, string version)
        {
            if (originalStatus.GetType() != currentStatus.GetType())
                throw new ArgumentException("the current status has a different type than original status, verify its type before applying a current status.");

            this.OnInit(id, indexName, typeName, originalStatus, currentStatus, serializer, origin, version);
        }

        /// <summary>
        /// Called when [initialize].
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="originalStatus">The original status.</param>
        /// <param name="currentStatus">The current status.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="version">The version.</param>
        /// <exception cref="System.ArgumentException">
        /// The identifier cannot be null or empty.;id
        /// or
        /// The indexName cannot be null or empty.;indexName
        /// or
        /// The typeName cannot be null or empty.;typeName
        /// or
        /// The version cannot be null or empty.;version
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// originalStatus;The current status cannot be null.
        /// or
        /// currentStatus;The current status cannot be null.
        /// or
        /// serializer;The serializer cannot be null.
        /// </exception>
        private void OnInit(string id, string indexName, string typeName,
            object originalStatus, object currentStatus, Func<object, string> serializer, OriginContext origin, string version)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("The identifier cannot be null or empty.", "id");

            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("The indexName cannot be null or empty.", "indexName");

            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("The typeName cannot be null or empty.", "typeName");

            if (originalStatus == null)
                throw new ArgumentNullException("originalStatus", "The current status cannot be null.");

            if (currentStatus == null)
                throw new ArgumentNullException("currentStatus", "The current status cannot be null.");

            if (serializer == null)
                throw new ArgumentNullException("serializer", "The serializer cannot be null.");

            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("The version cannot be null or empty.", "version");

            this.Id = id;
            this.IndexName = indexName;
            this.TypeName = typeName;
            this.serializer = serializer;
            this.CurrentStatus = currentStatus;
            this.OriginalStatus = serializer.Invoke(originalStatus); // It's needed to set id property if this one exists..
            this.Origin = origin;
            this.InstanceType = currentStatus.GetType();
            this.Version = version;
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
        /// Gets the origin of the current instance (used for transactions, rollback or commit)
        /// <remarks>If the origin is New so rollback operation must be deleted, otherwise updated on commit.</remarks>
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

        //public override bool Equals(object obj)
        //{
        //    if (obj == null)
        //        return false;

        //    if (this.GetType() == obj.GetType())
        //        return this.GetHashCode() == obj.GetHashCode();

        //    return false;
        //}

        
        //public override int GetHashCode()
        //{
        //    return this.Id.GetHashCode() - (this.IndexName.GetHashCode() + this.TypeName.GetHashCode());
        //}

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Id: {0}, Index: {1}, TypeName: {2}, Version: {3}", this.Id, this.IndexName, this.TypeName, this.Version);
        }
    }
}
