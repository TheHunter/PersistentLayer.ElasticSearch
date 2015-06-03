using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    public class KeyGenStrategy
    {
        private readonly Func<dynamic, dynamic> nextFunc;

        private KeyGenStrategy(Type keyType, Func<dynamic, dynamic> nextFunc)
        {
            this.KeyType = keyType;
            this.nextFunc = nextFunc;
        }

        /// <summary>
        /// Gets the type of the key.
        /// </summary>
        /// <value>
        /// The type of the key.
        /// </value>
        public Type KeyType { get; private set; }

        internal dynamic NextValue(dynamic current)
        {
            return this.nextFunc.Invoke(current);
        }

        /// <summary>
        /// Ofs the specified next function.
        /// </summary>
        /// <typeparam name="TId">The type of the identifier.</typeparam>
        /// <param name="nextFunc">The next function.</param>
        /// <returns></returns>
        public static KeyGenStrategy Of<TId>(Func<TId, TId> nextFunc)
        {
            return new KeyGenStrategy(typeof(TId), lastKey => nextFunc.Invoke(lastKey));
        }
    }

}
