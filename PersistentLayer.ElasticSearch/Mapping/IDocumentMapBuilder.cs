using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public interface IDocumentMapBuilder
    {
        Type DocumenType { get; }
        
        IDocumentMapper Build(KeyGenType keyGenType = KeyGenType.Identity);
    }
}
