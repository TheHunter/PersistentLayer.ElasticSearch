using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nest;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Cache;
using PersistentLayer.ElasticSearch.Metadata;
using PersistentLayer.ElasticSearch.Resolvers;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.Metadata
{
    public class MetadataTester
    {
        [Fact]
        public void CompareInstances()
        {
            var comparer = new MetadataComparer();
            var metadata1 = new MetadataInfo("1", "myindex", "mytype", new object(), "4");
            var metadata2 = new MetadataInfo("1", "myindex", "mytype", new object(), "3");

            var instance = new StringBuilder();
            var metadata3 = new MetadataInfo("1", "myindex", "mytype", instance, "3");
            var metadata4 = new MetadataInfo("2", "myindex", "mytype", instance, "3");

            Assert.True(comparer.Equals(metadata1, metadata2));
            Assert.True(comparer.Equals(metadata1, metadata3));

            Assert.False(comparer.Equals(metadata3, metadata4));
        }

        [Fact]
        public void MetadataCacheTest()
        {
            var jsonSettings = MakeJsonSettings(MakeSettings("current"));
            Func<object, string> serializer = instance => JsonConvert.SerializeObject(instance, Formatting.None, jsonSettings);
            var evaluator = new MetadataEvaluator
            {
                Serializer = serializer,
                Merge = (source, dest) => JsonConvert.PopulateObject(serializer(source), dest)
            };

            var comparer = new MetadataComparer();
            var metadata1 = new MetadataWorker("1", "current", "mytype", new object(), evaluator, OriginContext.Newone, 1.ToString());
            var metadata2 = new MetadataWorker("1", "current", "mytype", new object(), evaluator, OriginContext.Newone, 2.ToString());

            Assert.True(comparer.Equals(metadata1, metadata2));

            var cache = new MetadataCache("current", MakeElasticClient("current"));
            Assert.True(cache.Attach(metadata1));
            Assert.False(cache.Attach(metadata1));
            Assert.False(cache.Attach(metadata2));
            Assert.True(cache.Attach(new MetadataWorker("1", "current_test", "mytype", new object(), evaluator, OriginContext.Storage, 1.ToString())));
            Assert.False(cache.Attach(new MetadataWorker("1", "current", "mytype", new object(), evaluator, OriginContext.Storage, 1.ToString())));

            var count = cache.MetadataExpression(workers => workers.Count());
            Assert.Equal(2, count);
        }

        private static ElasticClient MakeElasticClient(string defaultIndex)
        {
            var list = new List<Type>
            {
                typeof(QueryPathDescriptorBase<,,>)
            };

            var settings = MakeSettings(defaultIndex)
                .ExposeRawResponse();
            
            settings.SetJsonSerializerSettingsModifier(
                delegate(JsonSerializerSettings zz)
                {
                    zz.NullValueHandling = NullValueHandling.Ignore;
                    zz.MissingMemberHandling = MissingMemberHandling.Ignore;
                    zz.TypeNameHandling = TypeNameHandling.Auto;
                    zz.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
                    zz.ContractResolver = new DynamicContractResolver(settings);
                });

            return new ElasticClient(settings, null, new CustomNestSerializer(settings, list));
        }

        private static JsonSerializerSettings MakeJsonSettings(ConnectionSettings settings)
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    ContractResolver = new DynamicContractResolver(settings)
            };
        }

        private static ConnectionSettings MakeSettings(string defaultIndex)
        {
            var uri = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(uri, defaultIndex);
            return settings;
        }
    }
}
