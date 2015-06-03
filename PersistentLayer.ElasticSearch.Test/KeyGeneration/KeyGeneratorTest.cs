using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PersistentLayer.ElasticSearch.KeyGeneration;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.KeyGeneration
{
    public class KeyGeneratorTest
    {
        [Fact]
        public void MakeInstance()
        {
            var strategy = KeyGenStrategy.Of<int>(i => ++i);
            var generator = new ElasticKeyGenerator(strategy, 0, "myindex", "mytype");

            var res0 = generator.Next<int>();
            var res1 = generator.Next<int>();
            var res2 = generator.Next<int>();
            var res3 = generator.Next();

            Assert.NotNull(res0);
            Assert.NotNull(res1);
            Assert.NotNull(res2);
            Assert.NotNull(res3);
        }
    }
}
