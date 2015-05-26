using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Nest;

namespace PersistentLayer.ElasticSearch.Extensions
{
    /// <summary>
    /// Query descriptor extension.
    /// </summary>
    public static class QueryDescriptorExtension
    {
        /// <summary>
        /// Terms the specified field descriptor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="fieldDescriptor">The field descriptor.</param>
        /// <param name="findValue">The find value.</param>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="boost">The boost.</param>
        /// <returns></returns>
        public static QueryContainer Term<T>(
            this QueryDescriptor<T> descriptor, Expression<Func<T, object>> fieldDescriptor,
            Func<object> findValue,
            bool condition = true,
            double? boost = null)
            where T : class
        {
            if (!condition)
                return null;

            var container = descriptor.Term(fieldDescriptor, findValue.Invoke());
            return container;
        }

        /// <summary>
        /// Matches the phrase.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="findValue">The find value.</param>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="boost">The boost.</param>
        /// <returns></returns>
        public static QueryContainer MatchPhrase<T>(this QueryDescriptor<T> descriptor, string fieldName,
            Func<string> findValue,
            bool condition = true,
            double? boost = null)
            where T : class
        {
            if (!condition)
                return null;

            var container = descriptor.MatchPhrase(queryDescriptor => queryDescriptor
                .OnField(fieldName)
                .Query(findValue.Invoke())
                );

            return container;
        }
    }
}
