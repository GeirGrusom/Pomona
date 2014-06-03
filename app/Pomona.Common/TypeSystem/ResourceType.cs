#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2014 Karsten Nikolai Strand
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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Pomona.Common.Internals;

namespace Pomona.Common.TypeSystem
{
    public class ResourceType : TransformedType
    {
        private static readonly MethodInfo convertToPathEncodedStringMethod =
            ReflectionHelper.GetMethodDefinition<ResourceType>(x => ConvertToPathEncodedString(null));

        private static readonly MethodInfo stringBuilderAppendFormatMethod =
            ReflectionHelper.GetMethodDefinition<StringBuilder>(
                x => x.AppendFormat((IFormatProvider)null, "", new object[] { }));

        private readonly Lazy<ResourceTypeDetails> resourceTypeDetails;
        private readonly Lazy<ResourceType> uriBaseType;
        private readonly Lazy<Action<object, StringBuilder>> uriGenerator;


        public ResourceType(IExportedTypeResolver typeResolver, Type type)
            : base(typeResolver, type)
        {
            this.uriBaseType = CreateLazy(() => typeResolver.LoadUriBaseType(this));
            this.resourceTypeDetails = CreateLazy(() => typeResolver.LoadResourceTypeDetails(this));
            this.uriGenerator = CreateLazy(() => BuildUriGenerator(this).Compile());
        }


        public PropertyMapping ChildToParentProperty
        {
            get { return ResourceTypeDetails.ChildToParentProperty; }
        }

        public bool IsExposedAsRepository
        {
            get { return ResourceTypeDetails.IsExposedAsRepository; }
        }

        public bool IsRootResource
        {
            get { return ResourceTypeDetails.ParentResourceType == null; }
        }

        public bool IsUriBaseType
        {
            get { return UriBaseType == this; }
        }

        public IEnumerable<ResourceType> MergedTypes
        {
            get { return SubTypes.OfType<ResourceType>(); }
        }

        public ResourceType ParentResourceType
        {
            get { return ResourceTypeDetails.ParentResourceType; }
        }

        public PropertyMapping ParentToChildProperty
        {
            get { return ResourceTypeDetails.ParentToChildProperty; }
        }

        public TransformedType PostReturnType
        {
            get { return ResourceTypeDetails.PostReturnType; }
        }

        public IEnumerable<Type> ResourceHandlers
        {
            get { return ResourceTypeDetails.ResourceHandlers; }
        }

        public ResourceType UriBaseType
        {
            get { return this.uriBaseType.Value; }
        }

        public string UriRelativePath
        {
            get { return ResourceTypeDetails.UriRelativePath; }
        }

        public string UriTemplate
        {
            get
            {
                var sb = new StringBuilder();
                BuildUriTemplate(this, sb, string.Empty);
                return sb.ToString();
            }
        }

        protected ResourceTypeDetails ResourceTypeDetails
        {
            get { return this.resourceTypeDetails.Value; }
        }


        public void AppendUri(object o, StringBuilder sb)
        {
            this.uriGenerator.Value(o, sb);
        }


        public string ToUri(object o)
        {
            var sb = new StringBuilder();
            AppendUri(o, sb);
            return sb.ToString();
        }


        private static Expression<Action<object, StringBuilder>> BuildUriGenerator(ResourceType rt)
        {
            var parameterExpression = Expression.Parameter(typeof(object), "x");
            var objParam = Expression.Convert(parameterExpression, rt);
            var sbParam = Expression.Parameter(typeof(StringBuilder), "sb");
            var sbArgs = new List<Expression>();
            var formatStringBuilder = new StringBuilder();
            BuildUriGenerator(rt, sbArgs, objParam, formatStringBuilder);
            var sbArgsEncoded =
                sbArgs.Select(
                    GetUrlPathEncodedExpression);

            return Expression.Lambda<Action<object, StringBuilder>>(Expression.Call(sbParam,
                stringBuilderAppendFormatMethod,
                Expression.Constant(CultureInfo.InvariantCulture),
                Expression.Constant(formatStringBuilder.ToString()),
                Expression.NewArrayInit(typeof(object), sbArgsEncoded)),
                parameterExpression,
                sbParam);
        }


        private static void BuildUriGenerator(ResourceType rt,
            List<Expression> sbFormatArgs,
            Expression parentExpression,
            StringBuilder formatStringBuilder)
        {
            if (rt.ParentToChildProperty != null)
            {
                var nextParentExpr = rt.ChildToParentProperty.CreateGetterExpression(parentExpression);
                BuildUriGenerator(rt.ParentResourceType, sbFormatArgs, nextParentExpr, formatStringBuilder);
                var sbArgsExpr = rt.PrimaryId.CreateGetterExpression(parentExpression);
                formatStringBuilder.AppendFormat("/{0}/{{{1}}}", rt.ParentToChildProperty.UriName, sbFormatArgs.Count);
                sbFormatArgs.Add(sbArgsExpr);
            }
            else
            {
                var sbArgsExpr = rt.PrimaryId.CreateGetterExpression(parentExpression);
                formatStringBuilder.AppendFormat("{0}/{{{1}}}", rt.UriRelativePath, sbFormatArgs.Count);
                sbFormatArgs.Add(sbArgsExpr);
            }
        }


        private static void BuildUriTemplate(ResourceType rt,
            StringBuilder sb,
            string parentPath,
            bool useJsonNameStyle = false)
        {
            if (rt.ParentToChildProperty != null)
            {
                BuildUriTemplate(rt.ParentResourceType,
                    sb,
                    string.Format("{0}{1}.",
                        parentPath,
                        useJsonNameStyle ? rt.ChildToParentProperty.JsonName : rt.ChildToParentProperty.Name));
                sb.AppendFormat("/{0}/{{{1}{2}}}",
                    rt.ParentToChildProperty.UriName,
                    parentPath,
                    rt.PrimaryId.Name);
            }
            else
                sb.AppendFormat("{0}/{{{1}{2}}}", rt.UriRelativePath, parentPath,
                    useJsonNameStyle ? rt.PrimaryId.JsonName : rt.PrimaryId.Name);
        }


        private static string ConvertToPathEncodedString(object o)
        {
            return HttpUtility.UrlPathSegmentEncode(Convert.ToString(o, CultureInfo.InvariantCulture));
        }


        private static Expression GetUrlPathEncodedExpression(Expression expr)
        {
            if (expr.Type == typeof(int))
                return Expression.Convert(expr, typeof(object));

            return Expression.Call(convertToPathEncodedStringMethod,
                Expression.Convert(expr, typeof(object)));
        }


        protected internal virtual ResourceType OnLoadUriBaseType()
        {
            return this;
        }
    }
}