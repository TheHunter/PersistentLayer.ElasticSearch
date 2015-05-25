using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nest;

namespace PersistentLayer.ElasticSearch
{
    public interface IElasticTransactionProvider
        : ITransactionProvider
    {
        IElasticClient Client { get; }

        IElasticSession Session { get; }
    }
}
