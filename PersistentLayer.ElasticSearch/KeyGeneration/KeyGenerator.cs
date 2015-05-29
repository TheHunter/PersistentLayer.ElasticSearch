using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    /// <summary>
    /// Rappresents a strategy for making documents identifiers.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    public abstract class KeyGenerator<TId>
        : IKeyGenerator
    {
        private readonly object locker = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGenerator{TId}"/> class.
        /// </summary>
        /// <param name="lastId">The last identifier.</param>
        protected KeyGenerator(TId lastId)
        {
            this.LastId = lastId;
        }

        /// <summary>
        /// Nexts this instance.
        /// </summary>
        /// <returns></returns>
        public TId Next()
        {
            lock (this.locker)
            {
                return this.NextId();
            }
        }

        /// <summary>
        /// Nexts the one.
        /// </summary>
        /// <returns></returns>
        public object NextOne()
        {
            return this.Next();
        }

        /// <summary>
        /// Gets the type of the key.
        /// </summary>
        /// <value>
        /// The type of the key.
        /// </value>
        public Type KeyType
        {
            get { return typeof(TId); }
        }

        /// <summary>
        /// Nexts the identifier.
        /// </summary>
        /// <returns></returns>
        protected abstract TId NextId();

        /// <summary>
        /// Gets or sets the last identifier.
        /// </summary>
        /// <value>
        /// The last identifier.
        /// </value>
        protected TId LastId { get; set; }
    }

    /// <summary>
    /// Rapprsents a basic definition for key generartor strategy.
    /// </summary>
    public interface IKeyGenerator
    {
        /// <summary>
        /// Nexts the one.
        /// </summary>
        /// <returns></returns>
        object NextOne();

        /// <summary>
        /// Gets the type of the key.
        /// </summary>
        /// <value>
        /// The type of the key.
        /// </value>
        Type KeyType { get; }
    }
}
