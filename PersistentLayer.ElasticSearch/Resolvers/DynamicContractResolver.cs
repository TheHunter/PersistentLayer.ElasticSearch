﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
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
            foreach (var prop in ctr.Properties)
            {
                prop.HasMemberAttribute = true;
            }
            return contract;
        }

        //protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        //{
        //    if (type == typeof(Attachment))
        //        Console.WriteLine();

        //    var properties = base.CreateProperties(type, memberSerialization);
        //    return properties;
        //}


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