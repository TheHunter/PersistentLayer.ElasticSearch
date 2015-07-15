using System;
using Newtonsoft.Json;

namespace PersistentLayer.ElasticSearch.Metadata
{
    public class ObjectEvaluator
        : IObjectEvaluator
    {
        private readonly Func<object, string> serializerFunc;
        private readonly JsonSerializerSettings serializerSettings;

        public ObjectEvaluator(JsonSerializerSettings serializerSettings)
        {
            this.serializerSettings = serializerSettings;
            this.serializerFunc = instance => JsonConvert.SerializeObject(instance, Formatting.None, serializerSettings);
        }

        public void Merge(object source, object destination)
        {
            var jsonSource = this.serializerFunc(source);
            this.Merge(jsonSource, destination);
        }

        public string Serialize(object source)
        {
            return this.serializerFunc(source);
        }

        public object Clone(object source)
        {
            var clone = Activator.CreateInstance(source.GetType(), true);
            this.Merge(source, clone);
            return clone;
        }

        //// implement a method named Override???... ce da valutare...

        public void Reset(object source)
        {
            var clone = Activator.CreateInstance(source.GetType(), true);
            this.Merge(clone, source);
        }

        private void Merge(string jsonSource, object destination)
        {
            JsonConvert.PopulateObject(jsonSource, destination, this.serializerSettings);
        }
    }
}
