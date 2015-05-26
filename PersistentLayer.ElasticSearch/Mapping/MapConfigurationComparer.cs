using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public class MapConfigurationComparer
         : IEqualityComparer<IMapConfiguration>
    {

        public bool Equals(IMapConfiguration x, IMapConfiguration y)
        {
            return this.GetHashCode(x) == this.GetHashCode(y);
        }

        public int GetHashCode(IMapConfiguration obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            if (obj.DocumenType == null)
                throw new ArgumentException("Document type cannot be null", "obj");

            return obj.DocumenType.GetHashCode();
        }
    }
}
