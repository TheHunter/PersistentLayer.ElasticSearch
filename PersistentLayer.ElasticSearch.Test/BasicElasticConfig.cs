using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Autofac;
using Autofac.Core;
using Nest;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Impl;
using PersistentLayer.ElasticSearch.KeyGeneration;
using PersistentLayer.ElasticSearch.Mapping;
using PersistentLayer.ElasticSearch.Metadata;
using PersistentLayer.ElasticSearch.Proxy;
using PersistentLayer.ElasticSearch.Resolvers;
using PersistentLayer.ElasticSearch.Test.Documents;

namespace PersistentLayer.ElasticSearch.Test
{
    public class BasicElasticConfig
    {
        private readonly IContainer container;

        public BasicElasticConfig()
        {
            var builder = new ContainerBuilder();
            const string uri = "http://localhost:9200";

            builder.Register(context => new Uri(uri))
                .AsSelf();

            builder.Register<Func<string, ConnectionSettings>>(delegate(IComponentContext context)
            {
                var current = context.Resolve<IComponentContext>();
                return index => new ConnectionSettings(current.Resolve<Uri>(), index);
            }
                );

            builder.RegisterType<KeyGeneratorResolver>()
                .SingleInstance()
                .AsSelf()
                .OnActivated(args => args.Instance
                    .Register(KeyGenStrategy.Of<int>(i => ++i))
                    .Register(KeyGenStrategy.Of<long>(i => ++i))
                    .Register(KeyGenStrategy.Of<double>(i => i + 1))
                    .Register(KeyGenStrategy.Of<int?>(i => ++i))
                );

            builder.Register(context => new ElasticInferrer(context.Resolve<Func<string, ConnectionSettings>>().Invoke("current")))
                .AsSelf();

            ////builder.Register(context => new ConnectionSettings())
            ////    .As<IConnectionSettingsValues>();

            // manca la registrazione del CustomIdResolver.. 

            builder.Register(
                context => new CustomIdResolver(context.Resolve<Func<string, ConnectionSettings>>().Invoke("current")))
                .AsSelf();

            builder.RegisterType<MapperDescriptorResolver>()
                .SingleInstance()
                .AsSelf()
                .OnActivated(delegate(IActivatedEventArgs<MapperDescriptorResolver> args)
                {
                    var instance = args.Instance;
                    var inferrer = args.Context.Resolve<ElasticInferrer>();
                    var idResolver = args.Context.Resolve<CustomIdResolver>();

                    instance.Register(
                        new MapperDescriptor<Person>(inferrer, idResolver)
                            .SurrogateKey(person => person.Cf)
                            .SetProperty(mapper => mapper.Version = mapper.MakeElasticProperty(person => person.Version))
                        );
                });

            builder.Register(context =>
                {
                    var jsonSettings = this.MakeJsonSettings(this.MakeSettings("current"));
                    jsonSettings.NullValueHandling = NullValueHandling.Include;
                    return new ObjectEvaluator(jsonSettings);
                })
            .As<IObjectEvaluator>();

            builder.Register(context => MakeModelBuilder())
                .AsSelf()
                .SingleInstance();

            builder.Register(context => new DocumentAdapterResolver(context.Resolve<ModuleBuilder>(), context.Resolve<IObjectEvaluator>().Merge, typeBuilder => typeBuilder.AddProperty<string>("$idsession")))
                .AsSelf()
                .SingleInstance();

            this.container = builder.Build();
        }

        private static ModuleBuilder MakeModelBuilder()
        {
            var appDomain = AppDomain.CurrentDomain;
            var myAsmName = new AssemblyName { Name = "MyDynamicAssembly" };

            AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(myAsmName,
                AssemblyBuilderAccess.RunAndCollect);

            return assemblyBuilder.DefineDynamicModule(myAsmName.Name, myAsmName.Name + ".dll");
        }

        protected ConnectionSettings MakeSettings(string defaultIndex)
        {
            return this.container.Resolve<Func<string, ConnectionSettings>>()
                .Invoke(defaultIndex);
        }

        protected IElasticTransactionProvider GetProvider(string defaultIndex)
        {
            return new ElasticTransactionProvider(this.MakeElasticClient(defaultIndex),
                this.container.Resolve<IObjectEvaluator>(),
                this.container.Resolve<CustomIdResolver>(),
                this.container.Resolve<KeyGeneratorResolver>(),
                this.container.Resolve<MapperDescriptorResolver>(),
                this.container.Resolve<DocumentAdapterResolver>()
                );
        }

        protected IElasticRootPagedDAO<object> MakePagedDao(string defaultIndex)
        {
            return new ElasticRootPagedDAO<object>(this.GetProvider(defaultIndex));
        }

        protected ElasticClient MakeElasticClient(string defaultIndex)
        {
            var list = new List<Type>
            {
                typeof(QueryPathDescriptorBase<,,>), typeof(MissingFilterDescriptor)
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
            //jsonSettings.TypeNameHandling = TypeNameHandling.Auto;
            jsonSettings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
            jsonSettings.ContractResolver = new DynamicContractResolver(connectionSettings);
        }
    }
}
