using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Nest;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Cache;
using PersistentLayer.ElasticSearch.Metadata;
using PersistentLayer.ElasticSearch.Test.Documents;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.Metadata
{
    public class MetadataTester
        : BasicElasticConfig
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
        public void CompareMetadataForCache()
        {
            var jsonSettings = this.MakeJsonSettings(this.MakeSettings("current"));
            jsonSettings.NullValueHandling = NullValueHandling.Include;

            Func<object, string> serializer = instance => JsonConvert.SerializeObject(instance, Formatting.None, jsonSettings);
            var evaluator = new MetadataEvaluator
            {
                Serializer = serializer,
                Merge = (source, dest) => JsonConvert.PopulateObject(serializer(source), dest)
            };

            var metadata1 = new MetadataWorker("1", "current", "mytype",
                new Person { Name = "myname" },
                evaluator,
                OriginContext.Newone,
                1.ToString(CultureInfo.InvariantCulture));

            var metadata2 = new MetadataWorker("1", "current", "mytype",
                new Person { Surname = "mysurname_updated" },
                evaluator,
                OriginContext.Newone,
                2.ToString(CultureInfo.InvariantCulture));

            metadata1.Update(metadata2);

            var doc = metadata1.Instance as Person;
            Assert.NotNull(doc);

            Assert.Equal(null, doc.Name);
            Assert.Equal("mysurname_updated", doc.Surname);

            metadata1.Restore();
            Assert.Equal("myname", doc.Name);
            Assert.Equal(null, doc.Surname);
        }

        [Fact]
        public void MetadataCacheTest()
        {
            var jsonSettings = this.MakeJsonSettings(this.MakeSettings("current"));
            Func<object, string> serializer = instance => JsonConvert.SerializeObject(instance, Formatting.None, jsonSettings);
            var evaluator = new MetadataEvaluator
            {
                Serializer = serializer,
                Merge = (source, dest) => JsonConvert.PopulateObject(serializer(source), dest)
            };

            var comparer = new IndexMetadataComparer();
            var metadata1 = new MetadataWorker("1", "current", "mytype", new object(), evaluator, OriginContext.Newone, 1.ToString(CultureInfo.InvariantCulture));
            var metadata2 = new MetadataWorker("1", "current", "mytype", new object(), evaluator, OriginContext.Newone, 2.ToString(CultureInfo.InvariantCulture));
            var metadata3 = new MetadataWorker("1", "other_index", "mytype", new object(), evaluator, OriginContext.Newone, 2.ToString(CultureInfo.InvariantCulture));

            Assert.True(comparer.Equals(metadata1, metadata2));
            Assert.True(comparer.Equals(metadata3, metadata2));
            Assert.True(comparer.Equals(metadata1, metadata3));

            var cache = new MetadataCache("current", this.MakeElasticClient("current"));
            Assert.True(cache.Attach(metadata1));
            Assert.False(cache.Attach(metadata1));
            Assert.False(cache.Attach(metadata2));
            Assert.True(cache.Attach(metadata3));
            Assert.True(cache.Attach(new MetadataWorker("1", "current_test", "mytype", new object(), evaluator, OriginContext.Storage, 1.ToString(CultureInfo.InvariantCulture))));
            Assert.True(cache.Attach(new MetadataWorker("2", "current_test", "mytype", new object(), evaluator, OriginContext.Storage, 1.ToString(CultureInfo.InvariantCulture))));

            var count = cache.MetadataExpression(workers => workers.Count());
            var count2 = cache.MetadataExpression(workers => workers.Count(), "current_test");
            Assert.Equal(4, count);
            Assert.Equal(2, count2);
        }

        //[Fact]
        //public void StackTest()
        //{
        //    var stack = new Stack<string>();
        //    stack.Push("first");
        //    stack.Push("second");
        //    stack.Push("thirth");

        //    var res = stack.Pop();
        //    res = stack.Pop();
        //    res = stack.Pop();

        //    res = stack.Pop();
        //    res = stack.Pop();
        //    Assert.Null(res);

        //}
    }
}
