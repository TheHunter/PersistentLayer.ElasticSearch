using System;
using System.Linq.Expressions;
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
        /// <typeparam name="T">Document type</typeparam>
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
        /// <typeparam name="T">Document type</typeparam>
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


        /// <summary>
        /// Applies the session filter.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="sessionFieldName">Name of the session field.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns></returns>
        public static SearchDescriptor<TEntity> ApplySessionFilter<TEntity>(this SearchDescriptor<TEntity> descriptor, string sessionFieldName, string sessionId)
            where TEntity : class
        {
            return descriptor.Filter(fd => fd
                .Or(fd1 => fd1.Missing(sessionFieldName),
                    fd2 => fd2.And(
                        fd22 => fd22.Exists(sessionFieldName),
                        fd23 => fd23.Term(sessionFieldName, sessionId)
                        )
                )
                );
        }
    }
}
