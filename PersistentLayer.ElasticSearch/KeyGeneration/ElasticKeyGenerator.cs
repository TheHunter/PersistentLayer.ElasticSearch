using System;
using PersistentLayer.ElasticSearch.Extensions;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.KeyGeneration
{
    public class ElasticKeyGenerator
    {
        private readonly object locker = new object();
        private readonly KeyGenStrategy keyGen;
        private object lastValue;

        public ElasticKeyGenerator(KeyGenStrategy keyGen, object lastValue, string index, string typeName)
        {
            if (keyGen == null)
                throw new ArgumentNullException("keyGen");

            lastValue = lastValue ?? keyGen.KeyType.GetDefaultValue();

            if (lastValue == null)
                throw new ArgumentNullException("lastValue");

            if (string.IsNullOrWhiteSpace(index))
                throw new ArgumentException("Index name not valid.", "index");

            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name not valid.", "typeName");

            this.keyGen = keyGen;
            this.Index = index;
            this.TypeName = typeName;
            this.lastValue = lastValue;
        }

        public object Next()
        {
            lock (this.locker)
            {
                this.lastValue = this.keyGen.NextValue(this.lastValue);
                return this.lastValue;
            }
        }

        public string Index { get; private set; }

        public string TypeName { get; private set; }

        public Type KeyType
        {
            get { return this.keyGen.KeyType; }
        }

        public TId Next<TId>()
        {
            // verificare se il tipo TId eè compatibile con il KeyType.
            lock (this.locker)
            {
                this.lastValue = this.keyGen.NextValue(this.lastValue);
                return this.lastValue as dynamic;
            }
        }

        public void Update(object lastVal)
        {
            if (this.keyGen.KeyType != lastVal.GetType())
                throw new InvalidIdentifierException("The given last key value is not consistent with this key generator.");

            lock (this.locker)
            {
                this.lastValue = lastVal;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this.GetType() == obj.GetType())
                return this.GetHashCode() == obj.GetHashCode();

            return false;
        }

        public override int GetHashCode()
        {
            return (this.Index.GetHashCode() + this.TypeName.GetHashCode()) - this.keyGen.KeyType.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Index: {0}, TypeName: {1}, ClrType: {2}", this.Index, this.TypeName, this.keyGen.KeyType.FullName);
        }
    }
}
