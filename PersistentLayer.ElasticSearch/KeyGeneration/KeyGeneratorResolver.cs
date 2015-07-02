using System;
using System.Collections.Generic;
using System.Linq;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    /// <summary>
    /// Rappresents a key generator container used for resolving identifier generators.
    /// </summary>
    public class KeyGeneratorResolver
        : IComponentResolver<KeyGenStrategy>
    {
        private readonly HashSet<KeyGenStrategy> keyGenerators;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGeneratorResolver"/> class.
        /// </summary>
        public KeyGeneratorResolver()
        {
            this.keyGenerators = new HashSet<KeyGenStrategy>();
        }

        /// <summary>
        /// Registers the specified key generator.
        /// </summary>
        /// <param name="keyGenerator">The key generator.</param>
        /// <returns></returns>
        public KeyGeneratorResolver Register(KeyGenStrategy keyGenerator)
        {
            if (!this.keyGenerators.Contains(keyGenerator))
                this.keyGenerators.Add(keyGenerator);
            
            return this;
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="TKeyType">The type of the identifier.</typeparam>
        /// <returns></returns>
        public KeyGenStrategy Resolve<TKeyType>()
        {
            return this.Resolve(typeof(TKeyType));
        }

        /// <summary>
        /// Resolves the specified type identifier.
        /// </summary>
        /// <param name="keyType">The type identifier.</param>
        /// <returns></returns>
        /// <exception cref="InvalidIdentifierException">Error</exception>
        public KeyGenStrategy Resolve(Type keyType)
        {
            var keyGenerator = this.keyGenerators.FirstOrDefault(generator => generator.KeyType == keyType);
            if (keyGenerator == null)
            {
                throw new InvalidIdentifierException(string.Format("The type of key generation cannot be resolved because the key type is not registered and cannot be resolved by this framework, It's needed to define a newone in order to register, type: {0}", keyType.Name));
            }
            return keyGenerator;
        }
    }
}
