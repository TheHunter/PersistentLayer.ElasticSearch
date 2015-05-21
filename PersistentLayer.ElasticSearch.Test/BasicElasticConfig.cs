using System;
using System.Collections.Generic;
using Nest;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Resolvers;

namespace PersistentLayer.ElasticSearch.Test
{
    public class BasicElasticConfig
    {
        protected ElasticClient MakeElasticClient(string defaultIndex)
        {
            var list = new List<Type>
            {
                typeof(QueryPathDescriptorBase<,,>)
            };

            var settings = this.MakeSettings(defaultIndex)
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

        protected JsonSerializerSettings MakeJsonSettings(ConnectionSettings settings)
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

        protected ConnectionSettings MakeSettings(string defaultIndex)
        {
            var uri = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(uri, defaultIndex);
            return settings;
        }
    }
}
