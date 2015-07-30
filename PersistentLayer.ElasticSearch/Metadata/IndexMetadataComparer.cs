using System;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// A metadata comparer used for indexing metadata evaluating metadata's identifier and type naming.
    /// </summary>
    public class IndexMetadataComparer
        : MetadataComparer
    {
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        /// <exception cref="NullReferenceException">The metadata used for computing hashcode cannot be null.</exception>
        /// <exception cref="ArgumentException">The implementation of metadata must initialize the following properties: { Id, IndexName, TypeName } ;obj</exception>
        public override int GetHashCode(IMetadataInfo obj)
        {
            if (obj == null)
                throw new NullReferenceException("The metadata used for computing hashcode cannot be null.");

            if (obj.Id == null || obj.TypeName == null)
                throw new ArgumentException("The implementation of metadata must initialize the following properties: { Id, IndexName, TypeName } ", "obj");

            return obj.Id.GetHashCode() - obj.TypeName.GetHashCode();
        }
    }
}
