using System;
using System.Linq;
using System.Reflection;
using Nest;
using PersistentLayer.ElasticSearch.Mapping;
using PersistentLayer.Exceptions;

namespace PersistentLayer.ElasticSearch.Extensions
{
    public static class ElasticClientExtension
    {
        
        public static bool DocumentExists(this IElasticClient client,
            string index, string type, object instance, params ConstraintValue[] properties)
        {
            if (properties == null || !properties.Any())
                return false;
            
            var result = client.Search(delegate(SearchDescriptor<object> descriptor)
            {
                descriptor.Index(index);
                descriptor.Type(type);
                descriptor.Take(1);

                foreach (var current in properties)
                {
                    descriptor.Query(qd => qd.Match(qdd => qdd.Query(current.PropertyValue)
                        .OnField(current.ElasticProperty)
                        ));
                }
                return descriptor;
            }
            );
            return result.Hits.Any();
        }

        /// <summary>
        /// Gets the identifier field mapping of the given document type.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public static IIdFieldMapping GetIdFieldMappingOf<TDocument>(this IElasticClient client, string index)
            where TDocument : class
        {
            var current = client.GetMapping<TDocument>(descriptor => descriptor
                .Index(index)
                );

            return (current == null || !current.IsValid) ? null : current.Mapping.IdFieldMappingDescriptor;
        }

        public static PropertyInfo GetIdPropertyInfoOf<TDocument>(this IElasticClient client, string index)
            where TDocument : class
        {
            var idFieldMapping = client.GetIdFieldMappingOf<TDocument>(index);
            if (idFieldMapping != null && idFieldMapping.Path != null)
            {
                const BindingFlags flags = BindingFlags.CreateInstance | BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.IgnoreCase;
                return typeof(TDocument).GetProperty(idFieldMapping.Path, flags);
            }
            return null;
        }

        public static IIdFieldMapping SetIdFieldMappingOf<TDocument>(this IElasticClient client, ElasticProperty property, string index)
            where TDocument : class
        {
            if (property == null)
                return null;

            if (!client.IndexExists(index).Exists)
            {
                var createResponse = client.CreateIndex(index);
                if (!createResponse.IsValid)
                    throw new BusinessLayerException("The id property wasn't created.", "GetIdFieldMappingOf");

                var mapper = client.Map<TDocument>(descriptor => descriptor
                    .Index(index)
                    .IdField(mappingDescriptor => mappingDescriptor.Path(property.ElasticName))
                    );

                if (!mapper.IsValid)
                    throw new BusinessLayerException("", "");

            }
            return client.GetIdFieldMappingOf<TDocument>(index);
        }

        public static object GetMaxValueOf<TDocument>(this IElasticClient client, ElasticProperty property, string index)
            where TDocument : class
        {
            return client.GetMaxValueOf(property, index, client.Infer.TypeName(typeof(TDocument)));
        }

        public static object GetMaxValueOf(this IElasticClient client, ElasticProperty property, string index, string typeName)
        {
            const string maxProp = "MaxProperty";
            var response = client.Search<object>(descriptor => descriptor
                .Index(index)
                .Type(typeName)
                .Aggregations(aggDescriptor => aggDescriptor
                    .Max(maxProp, aggregationDescriptor => aggregationDescriptor.Field(property.ElasticName))
                )
                );
            if (!response.IsValid)
                return null;

            var agg = response.Aggs.Max(maxProp);
            if (agg == null)
                return null;

            if (!agg.Value.HasValue)
                return null;

            var value = agg.Value.Value;
            if (value.GetType() == property.PropertyType)
                return value;

            return Convert.ChangeType(value, property.PropertyType.TryToUnboxType());
        }
    }
}
