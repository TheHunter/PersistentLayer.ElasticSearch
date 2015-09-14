using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PersistentLayer.ElasticSearch.Extensions;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.Extensions
{
    public class ReflectionExtensionTest
    {
        [Fact]
        public void IsPrimitiveNullableTest1()
        {
            Assert.True(typeof(int?).IsPrimitiveNullable());

            Assert.False(typeof(int).IsPrimitiveNullable());
        }
    }
}
