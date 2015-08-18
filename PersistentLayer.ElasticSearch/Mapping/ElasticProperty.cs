using System;
using System.Reflection;
using PersistentLayer.ElasticSearch.Extensions;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Rappresents a basic info about a particolar document property.
    /// </summary>
    public class ElasticProperty
    {
        private readonly Func<object, object> valueFunc;
        private readonly Action<object, object> valueAct;

        public ElasticProperty(PropertyInfo property, string elasticName)
        {
            if (property == null)
                throw new ArgumentNullException("property", "property used for ElasticProperty cannot be null.");

            if (string.IsNullOrWhiteSpace(elasticName))
                throw new ArgumentException("The name related to the given property info cannot be null.", "elasticName");

            this.Property = property;
            this.ElasticName = elasticName;

            this.valueFunc = instance => property.MakeGetter().DynamicInvoke(instance);
            this.valueAct = (instance, value) => property.MakeSetter().DynamicInvoke(instance, value);
        }

        public ElasticProperty(PropertyInfo property, string elasticName, Func<object, object> valueFunc)
        {
            if (property == null)
                throw new ArgumentNullException("property", "property used for ElasticProperty cannot be null.");

            if (string.IsNullOrWhiteSpace(elasticName))
                throw new ArgumentException("The name related to the given property info cannot be null.", "elasticName");

            if (valueFunc == null)
                throw new ArgumentNullException("valueFunc", "The delegate used for retreiving the value from property cannot be null.");

            this.Property = property;
            this.ElasticName = elasticName;
            this.valueFunc = valueFunc;
            this.valueAct = (instance, value) => property.MakeSetter().DynamicInvoke(instance, value);
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <value>
        /// The property.
        /// </value>
        protected PropertyInfo Property { get; private set; }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        /// <value>
        /// The type of the property.
        /// </value>
        public Type PropertyType
        {
            get { return this.Property.PropertyType; }
        }

        /// <summary>
        /// Gets the elasticName of the elastic.
        /// </summary>
        /// <value>
        /// The elasticName of the elastic.
        /// </value>
        public string ElasticName { get; private set; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        public object GetValue(object instance)
        {
            return this.valueFunc.Invoke(instance);
        }

        public TResult GetValue<TResult>(object instance)
        {
            var ret = this.valueFunc.Invoke(instance);
            if (ret == null)
                return default(TResult);

            Type current = instance.GetType();
            Type retType = typeof(TResult);

            if (retType.IsAssignableFrom(current))
                return ret as dynamic;

            if (retType == typeof(string))
                return ret.ToString() as dynamic;

            throw new InvalidCastException(
                string.Format("The property value cannot be converted into the given result type, TResult: {0}, TValue: {1}", retType.Name, current.Name));
        }

        public void SetValue(object instance, object value)
        {
            try
            {
                this.valueAct.Invoke(instance, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Impossible to assign the given value because It's not compatible.", ex);
            }

        }
    }
}
