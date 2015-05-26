using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Indicates a map document information.
    /// </summary>
    public interface IMapConfiguration
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        PropertyInfo Id { get; }

        /// <summary>
        /// Gets the type of the documen.
        /// </summary>
        /// <value>
        /// The type of the documen.
        /// </value>
        Type DocumenType { get; }

        /// <summary>
        /// Gets the surrogate key.
        /// </summary>
        /// <value>
        /// The surrogate key.
        /// </value>
        IEnumerable<PropertyInfo> SurrogateKey { get; }

        // manca la parte che riguarda la configurazione della generazione della chiave primaria,
        // i possibili valori potrebbero essere { native, assigned, identity, KeyGenProvider }
    }
}
