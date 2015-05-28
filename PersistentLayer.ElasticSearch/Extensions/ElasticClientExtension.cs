using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nest;
using PersistentLayer.ElasticSearch.Mapping;

namespace PersistentLayer.ElasticSearch.Extensions
{
    public static class ElasticClientExtension
    {
        public static bool DocumentExists(this IElasticClient client, IDocumentMapper docMapper, string index, object instance)
        {
            if (!docMapper.SurrogateKey.Any())
                return false;

            var result = client.Search(delegate(SearchDescriptor<object> descriptor)
            {
                descriptor.Index(index);
                descriptor.Type(docMapper.DocumenType);
                descriptor.Take(1);

                foreach (var elasticProperty in docMapper.SurrogateKey)
                {
                    ElasticProperty property = elasticProperty;
                    var propertyValue = property.ValueFunc.Invoke(instance).ToString();

                    descriptor.Query(qd => qd.Match(qdd => qdd.Query(propertyValue)
                        .OnField(property.ElasticName)
                        ));
                }
                return descriptor;
            }
            );
            return result.Hits.Any();
        }
    }
}
