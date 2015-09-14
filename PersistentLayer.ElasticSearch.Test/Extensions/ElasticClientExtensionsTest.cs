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
        [InlineData("currentforfind")]
        public void GetMaxValueOfProperty(string defaultIndex)
        {
            //var client = this.MakeElasticClient(defaultIndex);
            ////var ret = client.GetMaxValueOf(ElasticProperty.MakePropertyOf<Person>(person => person.Version, client.Infer), defaultIndex, "person");
            ////Assert.NotNull(ret);

            var client = new ElasticClient();

            const string maxProp = "MaxProperty";
            var response = client.Search<Person>(descriptor => descriptor
                .Index(defaultIndex)
                .Type("person")
                .Aggregations(aggDescriptor => aggDescriptor
                    //.Max(maxProp, aggregationDescriptor => aggregationDescriptor.Field("version"))
                    .Max("a", aggregationDescriptor => aggregationDescriptor.Field(o => o.Version))
                )
                );

            Assert.NotNull(response);
        }
    }
}
