using System;

namespace PersistentLayer.ElasticSearch.Metadata
{
    public class MetadataInfo
        : IMetadataInfo
    {
        public MetadataInfo(string id, string indexName, string typeName,
            object currentStatus, string version)
        {
            this.OnInit(id, indexName, typeName, currentStatus, currentStatus, version);
        }

        public MetadataInfo(string id, string indexName, string typeName,
            object originalStatus, object currentStatus, string version)
        {
            if (originalStatus.GetType() != currentStatus.GetType())
                throw new ArgumentException("the current status has a different type than original status, verify its type before applying a current status.");

            this.OnInit(id, indexName, typeName, originalStatus, currentStatus, version);
        }

        private void OnInit(string id, string indexName, string typeName,
            object originalStatus, object currentStatus, string version)
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

            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("The version cannot be null or empty.", "version");

            this.Id = id;
            this.IndexName = indexName;
            this.TypeName = typeName;
            this.Instance = currentStatus;
            this.Version = version;
        }

        public string Id { get; protected set; }

        public string IndexName { get; private set; }

        public string TypeName { get; private set; }

        public object Instance { get; private set; }

        public Type InstanceType
        {
            get { return this.Instance.GetType(); }
        }

        public string Version { get; protected set; }

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
