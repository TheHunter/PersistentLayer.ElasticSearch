using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using PersistentLayer.ElasticSearch.Extensions;
using PersistentLayer.ElasticSearch.Proxy;
using PersistentLayer.ElasticSearch.Test.Documents;
using Xunit;

namespace PersistentLayer.ElasticSearch.Test.Proxy
{
    public class ProxyGeneratorTest
    {
        private readonly ModuleBuilder moduleBuilder;

        public ProxyGeneratorTest()
        {
            var appDomain = AppDomain.CurrentDomain;
            var myAsmName = new AssemblyName { Name = "MyDynamicAssembly" };

            AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(myAsmName,
                AssemblyBuilderAccess.RunAndCollect);

            this.moduleBuilder = assemblyBuilder.DefineDynamicModule(myAsmName.Name, myAsmName.Name + ".dll");
        }

        [Theory]
        [InlineData("MyCustomProperty")]
        public void CreateProxy(string propertyname)
        {
            var proxy = this.moduleBuilder.BuildProxyFrom<Student>();
            Assert.NotNull(proxy);

            proxy.AddProperty<string>(propertyname);
            proxy.AddProperty<int>(propertyname + "__id");
            proxy.AddProperty(propertyname + "__id", typeof(int));

            Type derivedType = proxy.CreateType();
            Assert.NotNull(derivedType);

            Assert.True(typeof(Person).IsAssignableFrom(derivedType));
            Assert.False(derivedType.IsAssignableFrom(typeof(Student)));

            var property = derivedType.GetProperty(propertyname);
            Assert.NotNull(property);

            dynamic instance = Activator.CreateInstance(derivedType);
            Assert.NotNull(instance);

            //Assert.Null(property.GetValue(instance));

            //property.SetValue(instance, "my_value", null);

            instance.MyCustomProperty = "cioa....";
            
            Assert.NotNull(property.GetValue(instance));
            //Assert.Equal("my_value", property.GetValue(instance));
        }

        [Fact]
        public void WrongProxy()
        {
            Assert.Throws<InvalidOperationException>(() => this.moduleBuilder.BuildProxyFrom<MySealedClass>());
            Assert.Throws<InvalidOperationException>(() => this.moduleBuilder.BuildProxyFrom(typeof(MySealedClass)));
        }

        [Fact]
        public void MakeWrapper()
        {
            var wrapper = this.moduleBuilder.BuildWrapperFrom<MySealedClass>();
            Assert.NotNull(wrapper);

            Type wrapperType = wrapper.CreateType();
            Assert.NotNull(wrapperType);

            var instance = new MySealedClass(100)
            {
                Cf = "cf", Code = 1, Mcs = "mcs", MuCustomProp = 10, Name = "name", Surname = "surname"
            };
            dynamic newInstance = Activator.CreateInstance(wrapperType);

            Assert.NotNull(instance);
            Assert.NotNull(newInstance);

            newInstance.Cf = instance.Cf;
            newInstance.Code = instance.Code;
            
            newInstance.Mcs = instance.Mcs;
            newInstance.MuCustomProp = instance.MuCustomProp;
            newInstance.Name = instance.Name;
            newInstance.Surname = instance.Surname;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.CreateInstance
                | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.SetProperty;

            var property = wrapperType.GetProperty("Id", flags);
            Assert.NotNull(property);

            property.SetValue(newInstance, instance.Id);

            Assert.Equal(newInstance.Cf, instance.Cf);
            Assert.Equal(newInstance.Code, instance.Code);
            Assert.Equal(newInstance.MuCustomProp, instance.MuCustomProp);
            Assert.Equal(newInstance.Name, instance.Name);
            Assert.Equal(newInstance.Surname, instance.Surname);
            Assert.Equal(newInstance.Id, instance.Id);
        }
    }
}
