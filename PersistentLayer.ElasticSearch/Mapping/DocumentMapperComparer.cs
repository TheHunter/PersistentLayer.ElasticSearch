using System;
using System.Collections.Generic;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public class DocumentMapperComparer
         : IEqualityComparer<IDocumentMapper>
    {

        public bool Equals(IDocumentMapper x, IDocumentMapper y)
        {
            return this.GetHashCode(x) == this.GetHashCode(y);
        }

        public int GetHashCode(IDocumentMapper obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            if (obj.DocumentType == null)
                throw new ArgumentException("Document type cannot be null", "obj");

            return obj.DocumentType.GetHashCode();
        }
    }
}
