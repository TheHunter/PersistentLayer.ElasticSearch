using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    public class IntKeyGenerator
        : KeyGenerator<int>
    {
        public IntKeyGenerator(int lastValue)
            : base(lastValue)
        {
        }

        protected override int NextId()
        {
            return ++this.LastId;
        }
    }
}
