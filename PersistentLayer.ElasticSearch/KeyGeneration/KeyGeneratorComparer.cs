using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    /// <summary>
    /// 
    /// </summary>
    public class KeyGeneratorComparer
        : IEqualityComparer<IKeyGenerator>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(IKeyGenerator x, IKeyGenerator y)
        {
            return this.GetHashCode(x) == this.GetHashCode(y);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        /// <exception cref="ArgumentNullException">obj</exception>
        public int GetHashCode(IKeyGenerator obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            return obj.KeyType.GetHashCode();
        }
    }
}
