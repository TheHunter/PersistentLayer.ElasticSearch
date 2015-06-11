using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Metadata;
using PersistentLayer.ElasticSearch.Proxy;
using PersistentLayer.ElasticSearch.Test.Documents;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.Proxy
{
    public class DocumentAdapterTest
        : BasicElasticConfig
    {
        private readonly MetadataEvaluator evaluator;
        private readonly ModuleBuilder moduleBuilder;

        public DocumentAdapterTest()
        {
            var jsonSettings = this.MakeJsonSettings(this.MakeSettings("current"));
            jsonSettings.NullValueHandling = NullValueHandling.Include;

            Func<object, string> serializer = instance => JsonConvert.SerializeObject(instance, Formatting.None, jsonSettings);
            this.evaluator = new MetadataEvaluator
            {
                Serializer = serializer,
                Merge = (source, dest) => JsonConvert.PopulateObject(serializer(source), dest, jsonSettings)
            };

            //////////////////////////////////////////////////////////////////////////////////
            var appDomain = AppDomain.CurrentDomain;
            var myAsmName = new AssemblyName { Name = "MyDynamicAssembly" };

            AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(myAsmName,
                AssemblyBuilderAccess.RunAndCollect);

            this.moduleBuilder = assemblyBuilder.DefineDynamicModule(myAsmName.Name, myAsmName.Name + ".dll"); 
        }

        [Fact]
        public void Tester()
        {
            var proxy = this.moduleBuilder.BuildProxyFrom<Student>();
            Assert.NotNull(proxy);

            proxy.AddProperty<string>("IdSession");

            var proxyType = proxy.CreateType();
            var source = new Student(10)
            {
                Cf = "myCf", Code = 1500, Name = "myName", Surname = "mySurname"
            };

            var adapter = new DocumentAdapter(typeof(Student), proxyType, this.evaluator.Merge);
            Assert.NotNull(adapter);

            dynamic proxyInstance = adapter.MergeWith(source);
            Assert.NotNull(proxyInstance);


            proxyInstance.IdSession = "15415113156465461654";            

            Assert.Equal(source.Id, proxyInstance.Id);
            Assert.Equal(source.Cf, proxyInstance.Cf);
            Assert.Equal(source.Code, proxyInstance.Code);
            Assert.Equal(source.Name, proxyInstance.Name);
            Assert.Equal(source.Surname, proxyInstance.Surname);
            Assert.Equal("15415113156465461654", proxyInstance.IdSession);

        }

    }
}
