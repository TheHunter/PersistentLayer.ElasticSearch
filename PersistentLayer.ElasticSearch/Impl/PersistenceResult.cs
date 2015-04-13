using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class PersistenceResult<TInstance>
        : IPersistenceResult<TInstance>
        where TInstance : class
    {
        public bool IsValid { get; set; }
        public string Index { get; set; }
        public string Id { get; set; }
        public PersistenceType PersistenceType { get; set; }
        public string Error { get; set; }
    }

    public class PersistenceResult
        : PersistenceResult<object>, IPersistenceResult
    {
    }
}
