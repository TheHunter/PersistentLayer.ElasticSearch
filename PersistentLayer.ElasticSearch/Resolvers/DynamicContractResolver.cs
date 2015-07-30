using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Nest;
using Nest.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PersistentLayer.ElasticSearch.Resolvers
{
    /// <summary>
    /// 
    /// </summary>
    public class DynamicContractResolver
        : ElasticContractResolver
    {
        private static readonly Type DynamicType;
        private readonly List<MemberInfo> members;
        private readonly ElasticInferrer inferrer;

        /// <summary>
        /// 
        /// </summary>
        static DynamicContractResolver()
        {
            DynamicType = typeof(IDynamicMetaObjectProvider);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionSettings"></param>
        public DynamicContractResolver(IConnectionSettingsValues connectionSettings)
            : base(connectionSettings)
        {
            this.members = new List<MemberInfo>();
            this.inferrer = new ElasticInferrer(connectionSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);

            if (!DynamicType.IsAssignableFrom(objectType))
                return contract;

            /*
            NOTA:
                 * qui faccio una forzatura (alla proprietà che serve per la serializzazione)
                 * per tutti i membri dell'istanza dinamica,
                 * altrimenti i membri non etichettati non verrebbero serializzati.
            */
            
            dynamic ctr = contract;
            
            if (contract is JsonDynamicContract)
            {
                foreach (var prop in ctr.Properties)
                {
                    prop.HasMemberAttribute = true;
                }
            }
            else if (contract is JsonDictionaryContract)
            {
                Func<string, string> func = propertyName => this.inferrer.IndexName(propertyName);
                ctr.PropertyNameResolver = func;
            }
            
            return contract;
        }

        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var origMembers = base.GetSerializableMembers(objectType);
            this.members.AddRange(origMembers);
            return origMembers;
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            if (this.members.Any(info => info.Name.Equals(propertyName)))
                return base.ResolvePropertyName(propertyName);

            return propertyName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberSerialization"></param>
        /// <returns></returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (!(member is PropertyInfo))
                return property;

            dynamic prp = member;
            property.Writable = prp.GetSetMethod(true) != null;
            property.Readable = prp.GetGetMethod(true) != null;

            return property;
        }


    }
}