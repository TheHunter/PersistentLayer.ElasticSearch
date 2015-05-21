using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nest;

namespace PersistentLayer.ElasticSearch
{
    public interface IElasticSession
        : ISession
    {
        string Id { get; }

        string Index { get; }

        IElasticClient Client { get; }
    }
}
