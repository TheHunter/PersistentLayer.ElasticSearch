using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch
{
    public interface IComponentResolver<out TComponent>
    {
        TComponent Resolve<TFinder>();

        TComponent Resolve(Type typeFinder);
    }
}
