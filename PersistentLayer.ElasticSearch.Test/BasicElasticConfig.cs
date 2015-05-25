using System;
using System.Collections.Generic;
using Autofac;
using Nest;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Impl;
using PersistentLayer.ElasticSearch.Resolvers;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test
{
    public class BasicElasticConfig
    {
        private IContainer container;

        public BasicElasticConfig()
        {
            var builder = new ContainerBuilder();
            const string uri = "http://localhost:9200";
            //const string defaultIndex = "local";

            builder.Register(context => new Uri(uri))
                .AsSelf();

            builder.Register<Func<string, ConnectionSettings>>(delegate(IComponentContext context)
            {
                var current = context.Resolve<IComponentContext>();
                return index => new ConnectionSettings(current.Resolve<Uri>(), index);
            }
                );

            this.container = builder.Build();
        }
        

        protected ConnectionSettings MakeSettings(string defaultIndex)
        {
            //var uri = new Uri("http://localhost:9200");
            //var settings = new ConnectionSettings(uri, defaultIndex);
            //return settings;

            return this.container.Resolve<Func<string, ConnectionSettings>>()
                .Invoke(defaultIndex);
        }

        protected IElasticTransactionProvider GetProvider(string defaultIndex)
        {
            return new ElasticTransactionProvider(this.MakeElasticClient(defaultIndex), this.MakeJsonSettings(defaultIndex));
        }

        protected IElasticRootPagedDAO<object> MakePagedDao(string defaultIndex)
        {
            return new ElasticRootPagedDAO<object>(this.GetProvider(defaultIndex));
        }

        protected ElasticClient MakeElasticClient(string defaultIndex)
        {
            var list = new List<Type>
            {
                typeof(QueryPathDescriptorBase<,,>)
            };

            var settings = this.container.Resolve<Func<string, ConnectionSettings>>()
                .Invoke(defaultIndex);

            settings.SetJsonSerializerSettingsModifier(
                zz => this.JsonSettingsInit(zz, settings));

            return new ElasticClient(settings, null, new CustomNestSerializer(settings, list));
        }

        protected JsonSerializerSettings MakeJsonSettings(string defaultIndex)
        {
            var settings = this.container.Resolve<Func<string, ConnectionSettings>>()
                .Invoke(defaultIndex);

            return this.MakeJsonSettings(settings);
        }

        protected JsonSerializerSettings MakeJsonSettings(ConnectionSettings connectionSettings)
        {
            var settings = new JsonSerializerSettings();
            this.JsonSettingsInit(settings, connectionSettings);
            return settings;
        }

        internal void JsonSettingsInit(JsonSerializerSettings jsonSettings, ConnectionSettings connectionSettings)
        {
            jsonSettings.NullValueHandling = NullValueHandling.Ignore;
            jsonSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
            jsonSettings.TypeNameHandling = TypeNameHandling.Auto;
            jsonSettings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
            jsonSettings.ContractResolver = new DynamicContractResolver(connectionSettings);
        }
    }
}
