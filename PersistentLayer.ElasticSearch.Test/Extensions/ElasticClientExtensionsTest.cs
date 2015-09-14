using System;
using Elasticsearch.Net;
using Nest;
using PersistentLayer.ElasticSearch.Extensions;
using PersistentLayer.ElasticSearch.Mapping;
using PersistentLayer.ElasticSearch.Test.Documents;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.Extensions
{
    public class ElasticClientExtensionsTest
        : BasicElasticConfig
    {
        [Theory]
        [InlineData("currentforfindformax", "version")]
        public void GetMaxValueOfProperty(string defaultIndex, string fieldName)
        {
            var client = this.MakeElasticClient(defaultIndex);
            var ret = client.GetMaxValueOf(ElasticProperty.MakePropertyOf<Person>(person => person.Id, client.Infer), defaultIndex, "person");
            Assert.NotNull(ret);
        }
    }
}
