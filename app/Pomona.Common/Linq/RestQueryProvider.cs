﻿#region License

// ----------------------------------------------------------------------------
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

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Pomona.Common.Internals;
using Pomona.Common.Proxies;
using Pomona.Internals;

namespace Pomona.Common.Linq
{
    public class RestQueryProvider : IQueryProvider
    {
        private static readonly MethodInfo executeGenericMethod;
        private static readonly MethodInfo mapToCustomUserTypeResultMethod;
        private readonly IPomonaClient client;

        private readonly Type sourceType;
        private readonly string uri;
        private readonly static MethodInfo executeAsyncGenericMethod;


        static RestQueryProvider()
        {
            executeGenericMethod =
                ReflectionHelper.GetMethodDefinition<RestQueryProvider>(x => x.Execute<object>(null));
            executeAsyncGenericMethod =
                ReflectionHelper.GetMethodDefinition<RestQueryProvider>(x => x.ExecuteAsync<object>(null));
            mapToCustomUserTypeResultMethod =
                ReflectionHelper.GetMethodDefinition<RestQueryProvider>(
                    x => x.MapToCustomUserTypeResult<object>(null, null, null));
        }

        internal RestQueryProvider(IPomonaClient client, Type sourceType, string uri = null)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (sourceType == null)
                throw new ArgumentNullException("sourceType");
            this.client = client;
            this.sourceType = sourceType;
            this.uri = uri;
        }

        public string Uri
        {
            get { return uri; }
        }


        internal IPomonaClient Client
        {
            get { return client; }
        }


        IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
        {
            return new RestQuery<S>(this, expression);
        }


        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            var elementType = GetElementType(expression.Type);
            try
            {
                return
                    (IQueryable)
                    Activator.CreateInstance(
                        typeof (RestQuery<>).MakeGenericType(elementType),
                        new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }


        S IQueryProvider.Execute<S>(Expression expression)
        {
            return (S)Execute(expression);
        }


        object IQueryProvider.Execute(Expression expression)
        {
            return Execute(expression);
        }

        public virtual Task<object> ExecuteAsync(Expression expression)
        {
            // TODO: Support custom inherited client-side resources
            var queryTreeParser = new RestQueryableTreeParser();
            queryTreeParser.Visit(expression);
            return (Task<object>)executeAsyncGenericMethod.MakeGenericMethod(queryTreeParser.SelectReturnType).Invoke(
                this, new object[] { queryTreeParser });
        }

        public virtual object Execute(Expression expression)
        {
            object result;
            if (TryQueryCustomUserTypeIfRequired(expression, sourceType, out result))
                return result;

            var queryTreeParser = new RestQueryableTreeParser();
            queryTreeParser.Visit(expression);
            try
            {
                return executeGenericMethod.MakeGenericMethod(queryTreeParser.SelectReturnType).Invoke(
                    this, new object[] { queryTreeParser });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }


        public string GetQueryText(Expression expression)
        {
            return expression.ToString();
        }


        private static Type GetElementType(Type type)
        {
            var queryableTypeInstance = type.GetInterfacesOfGeneric(typeof (IQueryable<>)).FirstOrDefault();
            if (queryableTypeInstance == null)
                return type;

            return queryableTypeInstance.GetGenericArguments()[0];
        }


        private string BuildUri(RestQueryableTreeParser parser)
        {
            var builder = new UriQueryBuilder();

            var resourceInfo = client.GetResourceInfoForType(parser.ElementType);

            if (!resourceInfo.IsUriBaseType)
                builder.AppendParameter("$oftype", resourceInfo.JsonTypeName);

            SetProjection(parser, builder);

            if (parser.WherePredicate != null)
                builder.AppendExpressionParameter("$filter", parser.WherePredicate);
            if (parser.OrderKeySelector != null)
            {
                var sortOrder = parser.SortOrder;
                builder.AppendExpressionParameter(
                    "$orderby", parser.OrderKeySelector, x => sortOrder == SortOrder.Descending ? x + " desc" : x);
            }
            if (parser.GroupByKeySelector != null)
            {
                var selectBuilder = new QuerySelectBuilder(parser.GroupByKeySelector);
                builder.AppendParameter("$groupby", selectBuilder);
            }
            if (parser.SelectExpression != null)
            {
                var selectBuilder = new QuerySelectBuilder(parser.SelectExpression);
                builder.AppendParameter("$select", selectBuilder);
            }
            if (parser.SkipCount.HasValue)
                builder.AppendParameter("$skip", parser.SkipCount.Value);
            if (parser.TakeCount.HasValue)
                builder.AppendParameter("$top", parser.TakeCount.Value);

            var expandedPaths = parser.ExpandedPaths;
            if (!string.IsNullOrEmpty(expandedPaths))
                builder.AppendParameter("$expand", expandedPaths);

            if (parser.IncludeTotalCount)
                builder.AppendParameter("$totalcount", "true");

            return (uri ?? client.GetUriOfType(parser.ElementType)) + "?" + builder;
        }

        private static void SetProjection(RestQueryableTreeParser parser, UriQueryBuilder builder)
        {
            string projection = null;
            switch (parser.Projection)
            {
                case RestQueryableTreeParser.QueryProjection.First:
                case RestQueryableTreeParser.QueryProjection.FirstLazy:
                    projection = "first";
                    break;
                case RestQueryableTreeParser.QueryProjection.FirstOrDefault:
                    projection = "firstordefault";
                    break;
                case RestQueryableTreeParser.QueryProjection.Max:
                    projection = "max";
                    break;
                case RestQueryableTreeParser.QueryProjection.Min:
                    projection = "min";
                    break;
                case RestQueryableTreeParser.QueryProjection.Count:
                    projection = "count";
                    break;
                case RestQueryableTreeParser.QueryProjection.Sum:
                    projection = "sum";
                    break;
            }
            if (projection != null)
                builder.AppendParameter("$projection", projection);
        }


        private object Execute<T>(RestQueryableTreeParser parser)
        {
            var uri = BuildUri(parser);

            if (parser.Projection == RestQueryableTreeParser.QueryProjection.ToJson)
                return client.Get<JToken>(uri);

            if (parser.Projection == RestQueryableTreeParser.QueryProjection.ToUri)
                return new Uri(uri);

            if (parser.Projection == RestQueryableTreeParser.QueryProjection.FirstLazy)
            {
                var resourceInfo = client.GetResourceInfoForType(typeof (T));
                var proxy = (LazyProxyBase)Activator.CreateInstance(resourceInfo.LazyProxyType);
                proxy.Uri = uri;
                proxy.Client = client;
                return proxy;
            }


            switch (parser.Projection)
            {
                case RestQueryableTreeParser.QueryProjection.Enumerable:
                    return client.Get<IList<T>>(uri);
                case RestQueryableTreeParser.QueryProjection.First:
                    return GetFirst<T>(uri);
                case RestQueryableTreeParser.QueryProjection.FirstOrDefault:
                case RestQueryableTreeParser.QueryProjection.Max:
                case RestQueryableTreeParser.QueryProjection.Min:
                case RestQueryableTreeParser.QueryProjection.Sum:
                case RestQueryableTreeParser.QueryProjection.Count:
                    return client.Get<T>(uri);
                case RestQueryableTreeParser.QueryProjection.Any:
                    // TODO: Implement count querying without returning any results..
                    return client.Get<IList<T>>(uri).Count > 0;
                default:
                    throw new NotImplementedException("Don't recognize projection type " + parser.Projection);
            }
        }

        private async Task<object> ExecuteAsync<T>(RestQueryableTreeParser parser)
        {
            var uri = BuildUri(parser);
            /*
            if (parser.Projection == RestQueryableTreeParser.QueryProjection.ToUri)
                return new Uri(uri);
            
            if (parser.Projection == RestQueryableTreeParser.QueryProjection.FirstLazy)
            {
                var resourceInfo = client.GetResourceInfoForType(typeof(T));
                var proxy = (LazyProxyBase)Activator.CreateInstance(resourceInfo.LazyProxyType);
                proxy.Uri = uri;
                proxy.Client = client;
                return proxy;
            }*/


            switch (parser.Projection)
            {
                case RestQueryableTreeParser.QueryProjection.Enumerable:
                    return await client.GetAsync<IList<T>>(uri);
                case RestQueryableTreeParser.QueryProjection.FirstOrDefault:
                case RestQueryableTreeParser.QueryProjection.First:
                    return await client.GetAsync<T>(uri);
                case RestQueryableTreeParser.QueryProjection.Any:
                    // TODO: Implement count querying without returning any results..
                    return (await client.GetAsync<IList<T>>(uri)).Count > 0;
                default:
                    throw new NotImplementedException("Don't recognize projection type " + parser.Projection);
            }
        }


        private T GetFirst<T>(string uri)
        {
            try
            {
                return client.Get<T>(uri);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Sequence contains no matching element");
            }
        }
        private object MapToCustomUserTypeResult<TCustomClientType>(
            object result, CustomUserTypeInfo userTypeInfo, Expression transformedExpression)
        {
            Type elementType;

            if (transformedExpression.Type == userTypeInfo.ServerType)
            {
                return result != null ? (object)CreateClientSideResourceProxy<TCustomClientType>(userTypeInfo, result) : null;
            }

            if (transformedExpression.Type.TryGetEnumerableElementType(out elementType)
                && elementType == userTypeInfo.ServerType)
            {
                var wrappedResults =
                    (result as IEnumerable).Cast<object>()
                                           .Select(
                                               x => CreateClientSideResourceProxy<TCustomClientType>(userTypeInfo, x));
                // Map back to customClientType
                if (result is QueryResult)
                {
                    var resultAsQueryResult = (QueryResult)result;
                    return new QueryResult<TCustomClientType>(wrappedResults, resultAsQueryResult.Skip,
                                                              resultAsQueryResult.TotalCount, resultAsQueryResult.Url);
                }
                return wrappedResults.ToList();
            }
            // TODO!
            return result;
        }

        private TCustomClientType CreateClientSideResourceProxy<TCustomClientType>(CustomUserTypeInfo userTypeInfo,
                                                                                   object wrappedResource)
        {
            var proxy =
                (ClientSideResourceProxyBase)
                ((object)
                 RuntimeProxyFactory<ClientSideResourceProxyBase, TCustomClientType>.Create());
            proxy.Initialize(client, userTypeInfo, wrappedResource);
            return (TCustomClientType)((object)proxy);
        }


        private bool TryQueryCustomUserTypeIfRequired(Expression expression, Type customClientType, out object result)
        {
            CustomUserTypeInfo userTypeInfo;
            if (!CustomUserTypeInfo.TryGetCustomUserTypeInfo(customClientType, client, out userTypeInfo))
            {
                result = null;
                return false;
            }


            var visitor = new TransformAdditionalPropertiesToAttributesVisitor(client);
            var transformedExpression = visitor.Visit(expression);

            var nestedQueryProvider = new RestQueryProvider(client, userTypeInfo.ServerType, Uri);
            result = nestedQueryProvider.Execute(transformedExpression);

            result = mapToCustomUserTypeResultMethod.MakeGenericMethod(customClientType).Invoke(
                this, new[] { result, userTypeInfo, transformedExpression });

            return true;
        }
    }
}