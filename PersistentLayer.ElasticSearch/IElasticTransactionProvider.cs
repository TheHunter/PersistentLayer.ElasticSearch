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
