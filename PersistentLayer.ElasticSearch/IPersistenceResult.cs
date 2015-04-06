using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch
{
    public interface IPersistenceResult
        : IPersistenceResult<object>
    {
    }

    public interface IPersistenceResult<out TInstance>
        where TInstance : class 
    {
        bool IsValid { get; }

        string Index { get; }

        string Id { get; }

        TInstance Instance { get; }

        PersistenceType PersistenceType { get; }
        
        string Error { get; }
    }
}
