using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elasticsearch.Net.Serialization;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PersistentLayer.ElasticSearch
{
    /// <summary>
    /// Custom serializer for ES queries.
    /// </summary>
    public class CustomNestSerializer
        : NestSerializer
    {
        private readonly HashSet<Type> typesToInspect;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomNestSerializer"/> class.
        /// </summary>
        /// <param name="connectionSettings">The connection settings.</param>
        /// <param name="typesToInspect">The types to inspect.</param>
        public CustomNestSerializer(IConnectionSettingsValues connectionSettings, IEnumerable<Type> typesToInspect = null)
            : base(connectionSettings)
        {
            this.typesToInspect = new HashSet<Type>(typesToInspect ?? Enumerable.Empty<Type>());
        }

        /// <summary>
        /// Serializes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="formatting">The formatting.</param>
        /// <returns></returns>
        public override byte[] Serialize(object data, SerializationFormatting formatting = SerializationFormatting.Indented)
        {
            var format = formatting == SerializationFormatting.None ? Formatting.None : Formatting.Indented;

            if (data == null)
                return null;

            Type dataType = data.GetType();
            var binaryData = base.Serialize(data, formatting);

            if (!this.TypeToInspect(dataType))
                return binaryData;

            var originalJson = Encoding.UTF8.GetString(binaryData);
            var jObject = JObject.Parse(originalJson);

            var res = jObject.Remove("$type");
            res = this.RemoveTypeProperty(jObject) || res;
            return res ? Encoding.UTF8.GetBytes(jObject.ToString(format)) : binaryData;
        }

        /// <summary>
        /// Types to inspect.
        /// </summary>
        /// <param name="dataType">Type of the data.</param>
        /// <returns></returns>
        private bool TypeToInspect(Type dataType)
        {
            Type genType = null;
            Type genBaseType = null;
            if (dataType.IsGenericType)
            {
                genType = dataType.GetGenericTypeDefinition();
                if (dataType.BaseType != null && dataType.BaseType.IsGenericType)
                    genBaseType = dataType.BaseType.GetGenericTypeDefinition();
            }

            return this.typesToInspect.Any(type =>
                type.IsAssignableFrom(dataType)
                || type.IsAssignableFrom(genType)
                || type.IsAssignableFrom(genBaseType));
        }

        /// <summary>
        /// Removes the type property.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        private bool RemoveTypeProperty(JObject token)
        {
            var properties = token.Properties().Where(n => n.Value.Type == JTokenType.Object)
                .Select(n => n.Value as JObject)
                .ToArray();

            bool ret = token.Remove("$type");
            return properties.Aggregate(ret, (current, property) => this.RemoveTypeProperty(property) || current);
        }
    }
}
