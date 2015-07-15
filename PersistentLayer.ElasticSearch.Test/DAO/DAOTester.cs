using System;
using PersistentLayer.ElasticSearch.Test.Documents;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.DAO
{
    public class DAOTester
        : BasicElasticConfig
    {
        public DAOTester()
        {
            //var client = this.MakeElasticClient("current");
            //client.DeleteIndex(descriptor => descriptor.Index("current"));
        }

        [Theory]
        [InlineData("current")]
        public void FindByTest(string defaultIndex)
        {
            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var result = dao.FindAll<Person>();
                Assert.NotNull(result);
            }
        }

        [Theory]
        [InlineData("current")]
        public void MakePersistentTest(string defaultIndex)
        {
            var client = this.MakeElasticClient(defaultIndex);
            client.DeleteIndex(descriptor => descriptor.Index(defaultIndex));

            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var instance = new Person { Name = "Ton", Surname = "Jones", Cf = "mycf"};
                dao.MakePersistent(instance);
                
                var res = dao.FindBy<Person>(1);
                Assert.Null(res);

                var tranProvider = dao.GetTransactionProvider();
                tranProvider.BeginTransaction("first");
                dao.MakePersistent(instance);
                tranProvider.CommitTransaction();

                var res0 = dao.FindBy<Person>(1);
                Assert.NotNull(res0);
            }
        }

        [Theory]
        [InlineData("current")]
        public void MakePersistentTest2(string defaultIndex)
        {
            var client = this.MakeElasticClient(defaultIndex);
            client.DeleteIndex(descriptor => descriptor.Index(defaultIndex));

            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var instance = new Person { Name = "Ton", Surname = "Jones" };

                var tranProvider = dao.GetTransactionProvider();
                tranProvider.BeginTransaction("first");

                Assert.Throws<InvalidOperationException>(() => dao.MakePersistent(instance));
                instance.Cf = "kjdnfknskfn";
                dao.MakePersistent(instance);

                tranProvider.CommitTransaction();
                
                tranProvider.BeginTransaction();
                instance.Cf = "CFHGVHGSVDHJAVSHJVHGKV";
                
                // Asserts that the given instance is saved by auto dirty check.
                tranProvider.CommitTransaction();

                var res0 = dao.FindBy<Person>(1);
                Assert.NotNull(res0);
            }
        }

        [Theory]
        [InlineData("current")]
        public void MakePersistentTest3(string defaultIndex)
        {
            var client = this.MakeElasticClient(defaultIndex);
            client.DeleteIndex(descriptor => descriptor.Index(defaultIndex));

            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var instance = new Person { Name = "Ton", Surname = "Jones" };

                var tranProvider = dao.GetTransactionProvider();
                tranProvider.BeginTransaction("first");

                Assert.Throws<InvalidOperationException>(() => dao.MakePersistent(instance));
                instance.Cf = "kjdnfknskfn";
                dao.MakePersistent(instance);

                tranProvider.CommitTransaction();

                var res0 = dao.FindBy<Person>(1);
                Assert.NotNull(res0);
            }
        }
    }
}
