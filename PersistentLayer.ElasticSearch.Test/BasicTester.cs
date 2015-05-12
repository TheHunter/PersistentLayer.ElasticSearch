using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test
{
    /// <summary>
    /// 
    /// </summary>
    public class BasicTester
    {
        [Fact]
        public void TestOnAll()
        {
            var list = new List<string>
            {
                "ciao",
                "ciao",
                "no",
                "ciao",
                "ciao a tutti",
                "no ciao",
            };

            var list2 = new List<string>();
            var res = list.All(s =>
            {
                list2.Add(s);
                return s.Contains("ciao");
            }
                );

            Assert.False(res);
            Assert.Equal(6, list2.Count);
        }
    }
}
