using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    public class LongKeyGenerator
        : KeyGenerator<long>
    {

        public LongKeyGenerator(long lastValue)
            : base(lastValue)
        {
        }

        protected override long NextId()
        {
            return ++this.LastId;
        }
    }
}
