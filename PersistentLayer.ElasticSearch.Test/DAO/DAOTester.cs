﻿using PersistentLayer.ElasticSearch.Test.Documents;
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
        public void ExistTest(string defaultIndex)
        {
            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var instance = new Person(1) { Name = "Ton", Surname = "Jones" };
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
    }
}
