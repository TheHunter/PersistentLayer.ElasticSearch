using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    public class ExternalKeyGenerator<TId>
        : KeyGenerator<TId>
    {
        private readonly Func<TId> nextIdFunc;

        public ExternalKeyGenerator(TId lastId, Func<TId> nextIdFunc)
            : base(lastId)
        {
            if (nextIdFunc == null)
                throw new ArgumentNullException("nextIdFunc", "The delegate used for compute keys cannot be null.");

            this.nextIdFunc = nextIdFunc;
        }

        protected override TId NextId()
        {
            return this.nextIdFunc.Invoke();
        }
    }
}
