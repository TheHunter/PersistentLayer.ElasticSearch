using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PersistentLayer.ElasticSearch.Test.Documents;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test
{
    public class DAOTester
        : BasicElasticConfig
    {
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
    }
}
