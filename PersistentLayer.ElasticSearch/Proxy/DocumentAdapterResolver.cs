using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

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
        
        public DocumentAdapter Resolve<TFinder>()
        {
            return this.Resolve(typeof(TFinder));
        }

        public DocumentAdapter Resolve(Type typeFinder)
        {
            var adapter = this.adapters.FirstOrDefault(documentAdapter => documentAdapter.SourceType == typeFinder);
            if (adapter == null)
            {
                var typeBuilder = this.moduleBuilder.BuildProxyOrWrapper(typeFinder);
                if (this.beforeBuilding != null)
                {
                    this.beforeBuilding.Invoke(typeBuilder);
                }

                var adpterType = typeBuilder.CreateType();
                adapter = new DocumentAdapter(typeFinder, adpterType, this.mergerAction);
                this.adapters.Add(adapter);
            }

            return adapter;
        }
    }    
}
