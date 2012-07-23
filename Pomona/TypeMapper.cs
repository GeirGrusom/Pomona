#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2012 Karsten Nikolai Strand
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
using System.Linq;

namespace Pomona
{
    public class TypeMapper
    {
        private readonly ITypeMapperFilter filter;
        private readonly Dictionary<Type, IMappedType> mappings = new Dictionary<Type, IMappedType>();
        private HashSet<Type> sourceTypes;


        public TypeMapper(IEnumerable<Type> sourceTypes, ITypeMapperFilter filter = null)
        {
            this.filter = filter;
            this.sourceTypes = new HashSet<Type>(sourceTypes);
            foreach (var sourceType in this.sourceTypes)
                GetClassMapping(sourceType);
        }


        public ITypeMapperFilter Filter
        {
            get { return this.filter; }
        }

        public ICollection<Type> SourceTypes
        {
            get { return this.sourceTypes; }
        }

        public IEnumerable<TransformedType> TransformedTypes
        {
            get { return this.mappings.Values.OfType<TransformedType>(); }
        }


        public IMappedType GetClassMapping<T>()
        {
            var type = typeof(T);

            return GetClassMapping(type);
        }


        public IMappedType GetClassMapping(Type type)
        {
            IMappedType mappedType;
            if (!this.mappings.TryGetValue(type, out mappedType))
                mappedType = CreateClassMapping(type);

            return mappedType;
        }


        public bool IsSerializedAsArray(IMappedType mappedType)
        {
            if (mappedType == null)
                throw new ArgumentNullException("mappedType");
            return mappedType.IsCollection;
        }


        public bool IsSerializedAsArray(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            return IsSerializedAsArray(GetClassMapping(type));
        }


        public bool IsSerializedAsObject(IMappedType mappedType)
        {
            if (mappedType == null)
                throw new ArgumentNullException("mappedType");
            return mappedType is TransformedType;
        }


        public bool IsSerializedAsObject(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            return IsSerializedAsObject(GetClassMapping(type));
        }


        private IMappedType CreateClassMapping(Type type)
        {
            if (type.Assembly == typeof(String).Assembly || type.IsEnum ||
                type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                SharedType newSharedType;
                if (type.IsGenericType)
                {
                    newSharedType = new SharedType(type.GetGenericTypeDefinition(), this);
                    foreach (var genericTypeArg in type.GetGenericArguments())
                    {
                        if (genericTypeArg == type)
                        {
                            // Special case, self referencing generics
                            newSharedType.GenericArguments.Add(newSharedType);
                        }
                        else
                            newSharedType.GenericArguments.Add(GetClassMapping(genericTypeArg));
                    }
                }
                else
                    newSharedType = new SharedType(type, this);

                this.mappings[type] = newSharedType;
                return newSharedType;
            }

            if (this.sourceTypes.Contains(type))
            {
                var classDefinition = new TransformedType(type, type.Name, this);

                // Add to cache before filling out, in case of self-references
                this.mappings[type] = classDefinition;

                classDefinition.ScanProperties(type);

                return classDefinition;
            }

            // This is for proxyed types:
            if (type.BaseType != null && type.BaseType != typeof(object) && this.sourceTypes.Contains(type.BaseType))
            {
                // TODO: Improve heuristics for detecting proxy types
                var classDefinition = GetClassMapping(type.BaseType);
                this.mappings[type] = classDefinition;

                return classDefinition;
            }

            throw new InvalidOperationException("Don't know how to map " + type.FullName);
        }
    }
}