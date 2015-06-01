﻿using System;
using System.Collections.Generic;
using System.Linq;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    /// <summary>
    /// Rappresents a key generator container used for resolving identifiers.
    /// </summary>
    public class KeyGeneratorResolver
    {
        private readonly HashSet<IKeyGenerator> keyGenerators;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGeneratorResolver"/> class.
        /// </summary>
        public KeyGeneratorResolver()
        {
            var comparer = new KeyGeneratorComparer();
            this.keyGenerators = new HashSet<IKeyGenerator>(comparer);
        }

        /// <summary>
        /// Registers the specified key generator.
        /// </summary>
        /// <param name="keyGenerator">The key generator.</param>
        /// <returns></returns>
        public KeyGeneratorResolver Register(IKeyGenerator keyGenerator)
        {
            if (!this.keyGenerators.Contains(keyGenerator))
                this.keyGenerators.Add(keyGenerator);
            
            return this;
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="TId">The type of the identifier.</typeparam>
        /// <returns></returns>
        public IKeyGenerator Resolve<TId>()
        {
            return this.Resolve(typeof(TId));
        }

        /// <summary>
        /// Resolves the specified type identifier.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        /// <exception cref="InvalidIdentifierException">Error</exception>
        public IKeyGenerator Resolve(Type typeId)
        {
            var keyGenerator = this.keyGenerators.FirstOrDefault(generator => generator.KeyType == typeId);
            if (keyGenerator == null)
            {
                #region
                //if (typeId == typeof(int) || typeId == typeof(int?))
                //{
                //    keyGenerator = new IntKeyGenerator(0);
                //}
                //else if (typeId == typeof(long) || typeId == typeof(long?))
                //{
                //    keyGenerator = new LongKeyGenerator(0);
                //}
                //else
                //{
                //    throw new InvalidIdentifierException(string.Format("The type of key generation cannot be resolved because the key type is not registered and cannot be resolved by this framework, It's needed to define a newone in order to register, type: {0}", typeId.Name));
                //}
                #endregion

                throw new InvalidIdentifierException(string.Format("The type of key generation cannot be resolved because the key type is not registered and cannot be resolved by this framework, It's needed to define a newone in order to register, type: {0}", typeId.Name));
            }
            return keyGenerator;
        }
    }
}
