using PersistentLayer.Exceptions;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.DAO
{
    public class TransactionTester
        : BasicElasticConfig
    {
        [Theory]
        [InlineData("current")]
        public void BeginTransactionTest(string defaultIndex)
        {
            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var tranProvider = dao.GetTransactionProvider();

                tranProvider.BeginTransaction("first");
                //// one transaction is in progress..
                Assert.True(tranProvider.InProgress);

                //// no transaction in progress...
                tranProvider.RollbackTransaction();

                tranProvider.BeginTransaction("first");
                tranProvider.BeginTransaction("second");
                tranProvider.BeginTransaction("3");

                tranProvider.CommitTransaction();
                //// one transaction is in progress..
                Assert.True(tranProvider.InProgress);
                tranProvider.CommitTransaction();
                //// one transaction is in progress..
                Assert.True(tranProvider.InProgress);
                tranProvider.CommitTransaction();
                //// one transaction is in progress..
                Assert.False(tranProvider.InProgress);
            }
        }

        [Theory]
        [InlineData("current")]
        public void BeginTransactionTest2(string defaultIndex)
        {
            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var tranProvider = dao.GetTransactionProvider();
                tranProvider.BeginTransaction("first");

                // transaction with the indicated name exists.
                Assert.Throws<BusinessLayerException>(() => tranProvider.BeginTransaction("first"));

                tranProvider.RollbackTransaction();
            }
        }
    }
}
