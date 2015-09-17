using System;
using System.Collections.Generic;
using System.Linq;
using PersistentLayer.ElasticSearch.Test.Documents;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.DAO
{
    public class DAOTester
        : BasicElasticConfig
    {
        [Theory]
        [InlineData("currentforfind")]
        public void FindByTest(string defaultIndex)
        {
            var client = this.MakeElasticClient(defaultIndex);
            client.DeleteIndex(descriptor => descriptor.Index(defaultIndex));

            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var tranProvider = dao.GetTransactionProvider();
                tranProvider.BeginTransaction();
                var instance = new Person { Name = "Ton", Surname = "Jones", Cf = "mycf" };
                dao.MakePersistent(instance);
                tranProvider.CommitTransaction();

                // returns the document from local cache.
                Assert.NotNull(dao.FindBy<Person>(instance.Id));
                dao.Evict(instance);

                // returns the document from storage.
                Assert.NotNull(dao.FindBy<Person>(instance.Id));
            }

            client.DeleteIndex(descriptor => descriptor.Index(defaultIndex));
        }

        [Theory]
        [InlineData("currentforfind")]
        public void ExistsTest(string defaultIndex)
        {
            var client = this.MakeElasticClient(defaultIndex);
            client.DeleteIndex(descriptor => descriptor.Index(defaultIndex));
            client.Refresh(descriptor => descriptor.Force());

            var persons = new List<Person>();
            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var tranProvider = dao.GetTransactionProvider();
                tranProvider.BeginTransaction();
                persons.Add(new Person { Name = "Ton", Surname = "Jones", Cf = "mycf1" });
                persons.Add(new Person { Name = "Ton", Surname = "Jones", Cf = "mycf2" });

                dao.MakePersistent<Person>(persons);
                tranProvider.CommitTransaction();
            }

            using (var dao = this.MakePagedDao(defaultIndex))
            {
                Assert.True(dao.Exists<Person>(persons.Select(n => n.Id).ToList()));
            }
        }

        //[Theory]
        //[InlineData("currentforfind")]
        public void UniqueResultTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("currentforfind")]
        public void FindAllTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("currentforfind")]
        public void ExecuteExpressionTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("currentforfind")]
        public void GetPagedResultTest()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData("current")]
        public void MakePersistentTest(string defaultIndex)
        {
            var client = this.MakeElasticClient(defaultIndex);
            client.DeleteIndex(descriptor => descriptor.Index(defaultIndex));

            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var instance = new Person { Name = "Ton", Surname = "Jones", Cf = "mycf" };
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

                // throws a exception because It's missing the surrogate key (CF) set on the given instance
                Assert.Throws<InvalidOperationException>(() => dao.MakePersistent(instance));
                instance.Cf = "kjdnfknskfn";
                dao.MakePersistent(instance);

                tranProvider.CommitTransaction();

                var res0 = dao.FindBy<Person>(1);
                Assert.NotNull(res0);
            }

            using (var dao = this.MakePagedDao(defaultIndex))
            {
                var res1 = dao.FindBy<Person>(1);
                Assert.NotNull(res1);
            }
        }

        //[Theory]
        //[InlineData("current")]
        public void MakeTransientTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("current")]
        public void GetIdentifierTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("current")]
        public void IsCachedTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("current")]
        public void IsDirtyTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("current")]
        public void LoadTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("current")]
        public void SessionWithChangesTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("current")]
        public void EvictTest()
        {
            throw new NotImplementedException();
        }

        //[Theory]
        //[InlineData("current")]
        public void FlushTest()
        {
            throw new NotImplementedException();
        }
    }
}
