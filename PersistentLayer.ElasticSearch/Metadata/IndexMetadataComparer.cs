using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Metadata
{
    public class IndexMetadataComparer
        : MetadataComparer
    {
        public override int GetHashCode(IMetadataInfo obj)
        {
            if (obj == null)
                throw new NullReferenceException("The metadata used for computing hashcode cannot be null.");

            if (obj.Id == null || obj.TypeName == null)
                throw new ArgumentException("The implementation of metadata must initialize the following properties: { Id, IndexName, TypeName } ", "obj");

            return obj.Id.GetHashCode() - obj.TypeName.GetHashCode();
        }
    }
}
