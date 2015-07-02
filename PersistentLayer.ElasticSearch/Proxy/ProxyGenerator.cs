using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace PersistentLayer.ElasticSearch.Proxy
{
    /// <summary>
    /// Extension methods for building proxies and wrappers on compiled types.
    /// </summary>
    public static class ProxyGenerator
    {
        /// <summary>
        /// Determines whether this instance [can be proxy] the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static bool CanBeProxy(this Type type)
        {
            return !type.IsSealed;
        }

        /// <summary>
        /// Builds the proxy or wrapper.
        /// </summary>
        /// <param name="moduleBuilder">The module builder.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static TypeBuilder BuildProxyOrWrapper(this ModuleBuilder moduleBuilder, Type type)
        {
            return type.IsSealed ? moduleBuilder.BuildWrapperFrom(type) : moduleBuilder.BuildProxyFrom(type);
        }

        /// <summary>
        /// Builds the wrapper from.
        /// </summary>
        /// <typeparam name="TClass">The type of the class.</typeparam>
        /// <param name="moduleBuilder">The module builder.</param>
        /// <returns></returns>
        public static TypeBuilder BuildWrapperFrom<TClass>(this ModuleBuilder moduleBuilder)
        {
            return moduleBuilder.BuildWrapperFrom(typeof(TClass));
        }

        /// <summary>
        /// Builds the wrapper from.
        /// </summary>
        /// <param name="moduleBuilder">The module builder.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static TypeBuilder BuildWrapperFrom(this ModuleBuilder moduleBuilder, Type type)
        {
            var typeBuilder = moduleBuilder.DefineType(string.Format("{0}Wrapper", type.Name), type.Attributes);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.CreateInstance
                | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty;

            var properties = type.GetProperties(flags);

            foreach (var property in properties)
            {
                if (property.DeclaringType == type || property.DeclaringType == null)
                {
                    typeBuilder.AddProperty(property);
                }
                else
                {
                    typeBuilder.AddProperty(property.DeclaringType.GetProperty(property.Name, flags));
                }
            }
            return typeBuilder;
        }

        /// <summary>
        /// Builds the proxy from.
        /// </summary>
        /// <typeparam name="TClass">The type of the class.</typeparam>
        /// <param name="moduleBuilder">The module builder.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public static TypeBuilder BuildProxyFrom<TClass>(this ModuleBuilder moduleBuilder, TypeAttributes? attributes = null, string typeName = null)
        {
            return moduleBuilder.BuildProxyFrom(typeof(TClass), attributes, typeName);
        }

        /// <summary>
        /// Builds the proxy from.
        /// </summary>
        /// <param name="moduleBuilder">The module builder.</param>
        /// <param name="baseType">Type of the base.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static TypeBuilder BuildProxyFrom(this ModuleBuilder moduleBuilder, Type baseType, TypeAttributes? attributes = null, string typeName = null)
        {
            if (baseType.IsSealed)
                throw new InvalidOperationException(string.Format("Impossible to derive a sealed class, Name: {0}", baseType.Name));
            
            return moduleBuilder.DefineType(typeName ?? string.Format("{0}Proxy", baseType.Name), attributes == null ? baseType.Attributes : attributes.Value, baseType);
        }

        /// <summary>
        /// Builds the field.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldType">Type of the field.</param>
        /// <param name="fieldAttributes">The field attributes.</param>
        /// <returns></returns>
        public static FieldBuilder BuildField(this TypeBuilder typeBuilder, string fieldName, Type fieldType, FieldAttributes fieldAttributes)
        {
            return typeBuilder.DefineField(fieldName, fieldType, fieldAttributes);
        }

        /// <summary>
        /// Builds the method.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="name">The name.</param>
        /// <param name="methodAttributes">The method attributes.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns></returns>
        public static MethodBuilder BuildMethod(this TypeBuilder typeBuilder, string name, MethodAttributes methodAttributes, Type returnType, Type[] parameterTypes)
        {
            return typeBuilder.DefineMethod(name, methodAttributes, returnType, parameterTypes);
        }

        /// <summary>
        /// Builds the property.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="name">The name.</param>
        /// <param name="propertyAttributes">The property attributes.</param>
        /// <returns></returns>
        public static PropertyBuilder BuildProperty<TProperty>(this TypeBuilder typeBuilder, string name, PropertyAttributes propertyAttributes)
        {
            return typeBuilder.BuildProperty(name, propertyAttributes, typeof(TProperty));
        }

        /// <summary>
        /// Builds the property.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="name">The name.</param>
        /// <param name="propertyAttributes">The property attributes.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <returns></returns>
        public static PropertyBuilder BuildProperty(this TypeBuilder typeBuilder, string name, PropertyAttributes propertyAttributes, Type propertyType)
        {
            return typeBuilder.DefineProperty(name, propertyAttributes, propertyType, null);
        }

        /// <summary>
        /// Adds the property.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="name">The name.</param>
        /// <param name="propertyAttributes">The property attributes.</param>
        /// <returns></returns>
        public static TypeBuilder AddProperty<TProperty>(this TypeBuilder typeBuilder, string name, PropertyAttributes? propertyAttributes = null)
        {
            return typeBuilder.AddProperty(name, typeof(TProperty), propertyAttributes);
        }

        /// <summary>
        /// Adds the property.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyAttributes">The property attributes.</param>
        /// <returns></returns>
        public static TypeBuilder AddProperty(this TypeBuilder typeBuilder, string propertyName, Type propertyType, PropertyAttributes? propertyAttributes = null)
        {
            var propertyBuilder = typeBuilder.BuildProperty(propertyName, propertyAttributes == null ? PropertyAttributes.HasDefault : propertyAttributes.Value, propertyType);
            var fieldBuilder = typeBuilder.BuildField(propertyName, propertyType, FieldAttributes.Private);
            const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            MethodBuilder getPropertyBuilder =
                typeBuilder.BuildMethod("get_" + propertyName, getSetAttr, propertyType, Type.EmptyTypes);

            ILGenerator custNameGetIl = getPropertyBuilder.GetILGenerator();

            custNameGetIl.Emit(OpCodes.Ldarg_0);
            custNameGetIl.Emit(OpCodes.Ldfld, fieldBuilder);
            custNameGetIl.Emit(OpCodes.Ret);

            //---
            MethodBuilder setPropertyBuilder =
                typeBuilder.BuildMethod("set_" + propertyName, getSetAttr, null, new Type[] { propertyType });

            ILGenerator custNameSetIl = setPropertyBuilder.GetILGenerator();

            custNameSetIl.Emit(OpCodes.Ldarg_0);
            custNameSetIl.Emit(OpCodes.Ldarg_1);
            custNameSetIl.Emit(OpCodes.Stfld, fieldBuilder);
            custNameSetIl.Emit(OpCodes.Ret);

            //
            propertyBuilder.SetGetMethod(getPropertyBuilder);
            propertyBuilder.SetSetMethod(setPropertyBuilder);

            return typeBuilder;
        }

        /// <summary>
        /// Adds the property.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        public static TypeBuilder AddProperty(this TypeBuilder typeBuilder, PropertyInfo property)
        {
            var propertyBuilder = typeBuilder.BuildProperty(property.Name, property.Attributes, property.PropertyType);
            var fieldBuilder = typeBuilder.BuildField("_" + property.Name.ToLower(), property.PropertyType, FieldAttributes.Private);

            var getter = property.GetGetMethod() ?? property.GetGetMethod(true);
            if (getter != null)
            {
                MethodBuilder getPropertyBuilder =
                    typeBuilder.BuildMethod(getter.Name, getter.Attributes, property.PropertyType, Type.EmptyTypes);

                ILGenerator custNameGetIl = getPropertyBuilder.GetILGenerator();
                custNameGetIl.Emit(OpCodes.Ldarg_0);
                custNameGetIl.Emit(OpCodes.Ldfld, fieldBuilder);
                custNameGetIl.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(getPropertyBuilder);

                var setter = property.GetSetMethod() ?? property.GetSetMethod(true);
                if (setter != null)
                {
                    MethodBuilder setPropertyBuilder =
                        typeBuilder.BuildMethod(setter.Name, setter.Attributes, null, new Type[] { property.PropertyType });

                    ILGenerator custNameSetIl = setPropertyBuilder.GetILGenerator();

                    custNameSetIl.Emit(OpCodes.Ldarg_0);
                    custNameSetIl.Emit(OpCodes.Ldarg_1);
                    custNameSetIl.Emit(OpCodes.Stfld, fieldBuilder);
                    custNameSetIl.Emit(OpCodes.Ret);
                    propertyBuilder.SetSetMethod(setPropertyBuilder);
                }
            }

            return typeBuilder;
        }
    }

}
