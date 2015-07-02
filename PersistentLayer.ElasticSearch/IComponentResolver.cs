using System;

namespace PersistentLayer.ElasticSearch
{
    /// <summary>
    /// Rappresents a particolar resolver for retreiving instances.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component.</typeparam>
    public interface IComponentResolver<out TComponent>
    {
        /// <summary>
        /// Resolves a component associated with the given type.
        /// </summary>
        /// <typeparam name="TKeyType">The type of instance to retreive.</typeparam>
        /// <returns></returns>
        TComponent Resolve<TKeyType>();

        /// <summary>
        /// Resolves a component associated with the given type.
        /// </summary>
        /// <param name="keyType">The type of instance to retreive.</param>
        /// <returns></returns>
        TComponent Resolve(Type keyType);
    }
}
