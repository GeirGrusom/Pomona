﻿// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2013 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pomona.Common.Internals;

namespace Pomona.Common.Proxies
{
    public class ExtendedResourceBase : IHasResourceUri, IExtendedResourceProxy
    {
        public object WrappedResource { get; private set; }
        internal ExtendedResourceInfo UserTypeInfo { get; private set; }
        internal IClientTypeResolver Client { get; private set; }

        private static MethodInfo createProxyListMethod =
            ReflectionHelper.GetMethodDefinition<ExtendedResourceBase>(x => x.CreateProxyList<object>(null, null));

        internal void Initialize(IClientTypeResolver client, ExtendedResourceInfo userTypeInfo, object proxyTarget)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (userTypeInfo == null) throw new ArgumentNullException("userTypeInfo");
            if (proxyTarget == null) throw new ArgumentNullException("proxyTarget");
            Client = client;
            UserTypeInfo = userTypeInfo;
            this.WrappedResource = proxyTarget;
        }

        string IHasResourceUri.Uri
        {
            get { return ((IHasResourceUri) this.WrappedResource).Uri; }
            set { throw new NotSupportedException(); }
        }

        private IList<TElement> CreateProxyList<TElement>(IEnumerable source, ExtendedResourceInfo userTypeInfo)
        {
            return new List<TElement>(source.Cast<object>().Select(x =>
                {
                    var element = RuntimeProxyFactory<ExtendedResourceBase, TElement>.Create();
                    ((ExtendedResourceBase)((object)element)).Initialize(Client, userTypeInfo, x);
                    return element;
                }));
        }


        private static readonly MethodInfo onGetAttributeMethod =
            ReflectionHelper.GetMethodDefinition<ExtendedFormBase>(
                x => x.OnGetAttribute<object, object, object>(null));


        private static readonly MethodInfo onSetAttributeMethod =
            ReflectionHelper.GetMethodDefinition<ExtendedFormBase>(
                x => x.OnSetAttribute<object, object, object>(null, null));

        private Dictionary<string, object> nestedProxyCache = new Dictionary<string, object>();

        private bool IsServerKnownProperty<TOwner, TPropType>(PropertyWrapper<TOwner, TPropType> property)
        {
            return property.PropertyInfo.DeclaringType.IsInstanceOfType(this.WrappedResource);
        }

        protected TPropType OnGet<TOwner, TPropType>(PropertyWrapper<TOwner, TPropType> property)
        {
            if (IsServerKnownProperty(property))
                return property.Getter((TOwner) this.WrappedResource);

            // Check if this is a new'ed property on interface that is client type:
            ExtendedResourceInfo memberUserTypeInfo;
            if (ExtendedResourceInfo.TryGetExtendedResourceInfo(typeof(TPropType), Client, out memberUserTypeInfo))
            {
                var serverProp = UserTypeInfo.ServerType.GetResourceProperty(property.Name);
                if (serverProp != null && serverProp.PropertyType.IsAssignableFrom(typeof (TPropType)))
                {
                    var propValue = serverProp.GetValue(this.WrappedResource, null);
                    if (propValue == null)
                        return default(TPropType);
                    return (TPropType)nestedProxyCache.GetOrCreate(property.Name, () =>
                        {
                            var nestedProxy = RuntimeProxyFactory<ExtendedResourceBase, TPropType>.Create();
                            ((ExtendedResourceBase)((object)nestedProxy)).Initialize(Client, memberUserTypeInfo,
                                                                                            propValue);

                            return nestedProxy;
                        });
                }
            }

            Type elementType;
            if (typeof (TPropType).TryGetEnumerableElementType(out elementType) &&
                ExtendedResourceInfo.TryGetExtendedResourceInfo(elementType, Client, out memberUserTypeInfo))
            {

                var serverProp = UserTypeInfo.ServerType.GetResourceProperty(property.Name);
                if (serverProp != null)
                {
                    var propValue = serverProp.GetValue(this.WrappedResource, null);
                    if (propValue == null)
                        return default(TPropType);

                    return (TPropType)nestedProxyCache.GetOrCreate(property.Name, () => createProxyListMethod.MakeGenericMethod(elementType)
                                         .Invoke(this, new object[] { propValue, memberUserTypeInfo }));
                }
            }

            if (UserTypeInfo.DictProperty == null)
                throw new InvalidOperationException("No attributes property to map client-side property to!");

            var dictValueType = UserTypeInfo.DictProperty.PropertyType.GetGenericArguments()[1];
            return
                (TPropType)
                onGetAttributeMethod.MakeGenericMethod(typeof(TOwner), typeof(TPropType), dictValueType)
                                    .Invoke(this, new object[] { property });
        }


        private TPropType OnGetAttribute<TOwner, TPropType, TDictValue>(PropertyWrapper<TOwner, TPropType> property)
        {
            var dict = (IDictionary<string, TDictValue>)UserTypeInfo.DictProperty.GetValue(this.WrappedResource, null);
            TDictValue value;
            if (dict.TryGetValue(property.Name, out value))
                return (TPropType)((object)value);
            return default(TPropType);
        }

        protected void OnSet<TOwner, TPropType>(PropertyWrapper<TOwner, TPropType> property, TPropType value)
        {
            object unwrappedValue = value;
            var valueAsExtendedResource = value as ExtendedResourceBase;
            if (valueAsExtendedResource != null)
            {
                unwrappedValue = valueAsExtendedResource.WrappedResource;
            }

            if (IsServerKnownProperty(property))
            {
                property.Set((TOwner)this.WrappedResource, (TPropType)unwrappedValue);
                return;
            }

            if (typeof(IClientResource).IsAssignableFrom(typeof(TPropType)))
            {
                var underlyingServerProperty =
                    UserTypeInfo.ServerType.GetPropertySearchInheritedInterfaces(property.Name);
                if (underlyingServerProperty != null)
                {
                    underlyingServerProperty.SetValue(this.WrappedResource, unwrappedValue, null);
                    return;
                }
            }

            var dictValueType = UserTypeInfo.DictProperty.PropertyType.GetGenericArguments()[1];
            onSetAttributeMethod.MakeGenericMethod(typeof(TOwner), typeof(TPropType), dictValueType)
                                .Invoke(this, new object[] { property, value });
        }


        protected virtual bool OnSetAttribute<TOwner, TPropType, TDictValue>(PropertyWrapper<TOwner, TPropType> property,
                                                                   TPropType value)
        {
            var dict = (IDictionary<string, TDictValue>)UserTypeInfo.DictProperty.GetValue(this.WrappedResource, null);
            dict[property.Name] = (TDictValue)((object)value);
            return false;
        }
    }
}