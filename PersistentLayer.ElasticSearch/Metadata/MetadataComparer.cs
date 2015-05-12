using System;
using System.Collections.Generic;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Used for determinating if instances are considered equals.
    /// </summary>
    public class MetadataComparer
        : IEqualityComparer<IMetadataInfo>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(IMetadataInfo x, IMetadataInfo y)
        {
            if (x == null || y == null)
                return false;

            return x.GetHashCode() == y.GetHashCode();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        /// <exception cref="System.NullReferenceException">The metadata used for computing hashcode cannot be null.</exception>
        /// <exception cref="System.ArgumentException">The implementation of metadata must initialize the following properties: { Id, IndexName, TypeName } ;obj</exception>
        public int GetHashCode(IMetadataInfo obj)
        {
            if (obj == null)
                throw new NullReferenceException("The metadata used for computing hashcode cannot be null.");

            if (obj.Id == null || obj.IndexName == null || obj.TypeName == null)
                throw new ArgumentException("The implementation of metadata must initialize the following properties: { Id, IndexName, TypeName } ", "obj");

            return obj.Id.GetHashCode() - (obj.IndexName.GetHashCode() + obj.TypeName.GetHashCode());
        }
    }
}
