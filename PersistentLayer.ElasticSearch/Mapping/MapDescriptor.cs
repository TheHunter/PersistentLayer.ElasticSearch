using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace PersistentLayer.ElasticSearch.Mapping
{
    /// <summary>
    /// Prepares a custom map descriptor for making map configuration.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class MapDescriptor<TDocument>
        where TDocument : class
    {
        private readonly List<Action<MapConfiguration>> actions;

        internal MapDescriptor()
        {
            this.actions = new List<Action<MapConfiguration>>();
        }

        public MapDescriptor<TDocument> Id(Expression<Func<TDocument, object>> docExpression)
        {
            this.actions.Add(configuration => configuration.Id = docExpression.AsPropertyInfo());
            return this;
        }

        public MapDescriptor<TDocument> SurrogateKey(params Expression<Func<TDocument, object>>[] docExpression)
        {
            var list = docExpression.Select(expression => expression.AsPropertyInfo()).ToList();
            this.actions.Add(configuration => configuration.SurrogateKey = list);
            return this;
        }

        public IMapConfiguration Build()
        {
            var ret = new MapConfiguration(typeof(TDocument));
            this.actions.ForEach(action => action.Invoke(ret));
            return ret;
        }

        public void Reset()
        {
            this.actions.Clear();
        }

        /// <summary>
        /// Makes a map descriptor for the given document type.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <returns></returns>
        public static MapDescriptor<TDocument> For<TDocument>()
            where TDocument : class
        {
            return new MapDescriptor<TDocument>();
        }
    }
}
