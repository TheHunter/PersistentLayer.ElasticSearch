using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace PersistentLayer.ElasticSearch.Proxy
{
    public class DocumentAdapterResolver
        : IComponentResolver<DocumentAdapter>
    {
        private readonly ModuleBuilder moduleBuilder;
        private readonly Action<object, object> mergerAction;
        private readonly Action<TypeBuilder> beforeBuilding;
        private readonly HashSet<DocumentAdapter> adapters;

        public DocumentAdapterResolver(ModuleBuilder moduleBuilder, Action<object, object> mergerAction, Action<TypeBuilder> beforeBuilding = null)
        {
            this.moduleBuilder = moduleBuilder;
            this.mergerAction = mergerAction;
            this.beforeBuilding = beforeBuilding;
            this.adapters = new HashSet<DocumentAdapter>();
        }

        public DocumentAdapter Resolve<TKeyType>()
        {
            return this.Resolve(typeof(TKeyType));
        }

        public DocumentAdapter Resolve(Type keyType)
        {
            var adapter = this.adapters.FirstOrDefault(documentAdapter => documentAdapter.SourceType == keyType);
            if (adapter == null)
            {
                var typeBuilder = this.moduleBuilder.BuildProxyOrWrapper(keyType);
                if (this.beforeBuilding != null)
                {
                    this.beforeBuilding.Invoke(typeBuilder);
                }

                var adpterType = typeBuilder.CreateType();
                adapter = new DocumentAdapter(keyType, adpterType, this.mergerAction);
                this.adapters.Add(adapter);
            }

            return adapter;
        }
    }    
}
