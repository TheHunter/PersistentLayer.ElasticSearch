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
        [Theory]
        [InlineData(0)]
        public void KeyGenStrategyIntTest(int lastValue)
        {
            var strategy = KeyGenStrategy.Of<int>(i => ++i);
            var generator = new ElasticKeyGenerator(strategy, lastValue, "myindex", "mytype");

            var res0 = generator.Next<int>();
            var res1 = generator.Next<int>();
            var res2 = generator.Next<int>();
            dynamic res3 = generator.Next();

            Assert.Equal(lastValue + 1, res0);
            Assert.Equal(res0 + 1, res1);
            Assert.Equal(res1 + 1, res2);
            Assert.Equal(res2 + 1, res3);
        }

        [Theory]
        [InlineData(1.00, 0.02)]
        public void KeyGenStrategyDoubleTest(double lastValue, double step)
        {
            var strategy = KeyGenStrategy.Of<double>(i => i + step);
            var generator = new ElasticKeyGenerator(strategy, lastValue, "myindex", "mytype");

            var res0 = generator.Next<double>();
            var res1 = generator.Next<double>();
            var res2 = generator.Next<double>();
            dynamic res3 = generator.Next();

            Assert.Equal(lastValue + step, res0);
            Assert.Equal(res0 + step, res1);
            Assert.Equal(res1 + step, res2);
            Assert.Equal(res2 + step, res3);
        }
    }
}
