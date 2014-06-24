﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2014 Karsten Nikolai Strand
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

#endregion

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using Mono.Reflection;

using Newtonsoft.Json;

using Pomona.Common.TypeSystem;
using Pomona.Example.Models;

namespace Pomona.Example
{
    public class CritterTypeMappingFilter : TypeMappingFilterBase
    {
        public CritterTypeMappingFilter(IEnumerable<Type> sourceTypes)
            : base(sourceTypes)
        {
        }


        public override PropertyFlags? GetPropertyFlags(PropertyInfo propertyInfo)
        {
            if (propertyInfo.Name == "IsNotAllowedInFilters")
                return base.GetPropertyFlags(propertyInfo) & ~PropertyFlags.AllowsFiltering;
            return base.GetPropertyFlags(propertyInfo);
        }


        public override bool ClientPropertyIsExposedAsRepository(PropertyInfo propertyInfo)
        {
            if (propertyInfo.DeclaringType == typeof(Farm) && propertyInfo.Name == "Critters")
                return true;

            return base.ClientPropertyIsExposedAsRepository(propertyInfo);
        }


        public override string GetClientAssemblyName()
        {
            return "Critters.Client";
        }


        public override string GetClientInformationalVersion()
        {
            return "0.1.0-alpha1";
        }


        public override Type GetClientLibraryType(Type type)
        {
            if (type == typeof(WebColor))
                return typeof(string);

            return base.GetClientLibraryType(type);
        }


        public override LambdaExpression GetDecompiledPropertyFormula(Type type, PropertyInfo propertyInfo)
        {
            try
            {
                try
                {
                    Console.WriteLine("Decompiled instructions:");
                    var instructions = propertyInfo.GetGetMethod().GetInstructions();
                    Console.WriteLine(string.Join("\r\n", instructions));
                }
                catch
                {
                }
                return base.GetDecompiledPropertyFormula(type, propertyInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while decompiling {0}: {1}", propertyInfo.Name, ex);
                throw;
            }
        }


        public override JsonConverter GetJsonConverterForType(Type type)
        {
            if (type == typeof(WebColor))
                return new WebColorConverter();

            return base.GetJsonConverterForType(type);
        }


        public override Type GetUriBaseType(Type type)
        {
            if (typeof(Order).IsAssignableFrom(type))
                return typeof(Order);
            if (typeof(Weapon).IsAssignableFrom(type))
                return typeof(Weapon);
            if (type == typeof(MusicalCritter))
                return typeof(Critter);
            if (typeof(DictionaryContainer).IsAssignableFrom(type))
                return typeof(DictionaryContainer);

            if (type == typeof(EntityBase))
                return null;

            return base.GetUriBaseType(type);
        }


        public override bool PropertyIsAlwaysExpanded(Type type, PropertyInfo propertyInfo)
        {
            if (propertyInfo.DeclaringType == typeof(DictionaryContainer) && propertyInfo.Name == "Map")
                return true;
            if (propertyInfo.DeclaringType == typeof(OrderResponse) && propertyInfo.Name == "Order")
                return true;

            return base.PropertyIsAlwaysExpanded(type, propertyInfo);
        }


        public override Type ResolveRealTypeForProxy(Type type)
        {
            if (typeof(IExposedInterface).IsAssignableFrom(type))
                return typeof(IExposedInterface);
            return base.ResolveRealTypeForProxy(type);
        }


        public override bool TypeIsMapped(Type type)
        {
            if (type == typeof(ExcludedThing) || type == typeof(HiddenBaseInMiddle))
                return false;

            return base.TypeIsMapped(type);
        }


        public override bool TypeIsMappedAsSharedType(Type type)
        {
            if (type == typeof(WebColor))
                return true;

            return base.TypeIsMappedAsSharedType(type);
        }


        public override bool TypeIsMappedAsValueObject(Type type)
        {
            if (type == typeof(OrderResponse))
                return true;

            if (type == typeof(CrazyValueObject))
                return true;
            return base.TypeIsMappedAsValueObject(type);
        }
    }
}