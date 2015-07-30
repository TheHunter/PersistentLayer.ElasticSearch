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
