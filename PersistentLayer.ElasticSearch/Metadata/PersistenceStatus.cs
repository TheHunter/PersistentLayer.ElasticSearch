using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Metadata
{
    /// <summary>
    /// Status related about an instance present into session context.
    /// </summary>
    public enum PersistenceStatus
    {
        /// <summary>
        /// The transient status, which means Instance doesn't exists on session context.
        /// </summary>
        Transient = 1,

        /// <summary>
        /// The persistent statuc, which indicates Instance is associated into session context, and very probably on storage.
        /// </summary>
        Persistent = 2,

        /// <summary>
        /// The detached status, It could mean instances are present into storage, but the given status could be updated, or maybe deleted at all.
        /// </summary>
        Detached = 3
    }
}
