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
using System.Linq;
using System.Text;

using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Routing;

using Pomona.Common.Serialization;
using Pomona.Common.TypeSystem;
using Pomona.Routing;

namespace Pomona
{
    /// <summary>
    /// Default response processor base class for Pomona.
    /// </summary>
    public abstract class PomonaResponseProcessorBase : IResponseProcessor
    {
        private readonly IRouteCacheProvider routeCacheProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="PomonaResponseProcessorBase"/> class.
        /// </summary>
        /// <param name="routeCacheProvider">The route cache provider.</param>
        /// <exception cref="System.ArgumentNullException">routeCacheProvider</exception>
        protected PomonaResponseProcessorBase(IRouteCacheProvider routeCacheProvider)
        {
            if (routeCacheProvider == null)
                throw new ArgumentNullException("routeCacheProvider");

            this.routeCacheProvider = routeCacheProvider;
        }


        /// <summary>
        /// Gets a set of mappings that map a given extension (such as .json)
        /// to a media range that can be sent to the client in a vary header.
        /// </summary>
        public abstract IEnumerable<Tuple<string, MediaRange>> ExtensionMappings { get; }

        /// <summary>
        /// Gets the HTTP content type.
        /// </summary>
        /// <value>
        /// The HTTP content type.
        /// </value>
        protected abstract string ContentType { get; }


        /// <summary>
        /// Determines whether this instance can process the specified requested media range.
        /// </summary>
        /// <param name="requestedMediaRange">The requested media range.</param>
        /// <param name="model">The model.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public abstract ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context);


        /// <summary>
        /// Process the response
        /// </summary>
        /// <param name="requestedMediaRange">Content type requested by the client</param>
        /// <param name="model">The model for the given media range</param>
        /// <param name="context">The nancy context</param>
        /// <returns>A response</returns>
        public virtual Response Process(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            var pomonaResponse = (PomonaResponse)model;

            if (pomonaResponse.Entity == PomonaResponse.NoBodyEntity)
                return new Response { StatusCode = pomonaResponse.StatusCode };

            string jsonString = GetSerializerFactory(context)
                .GetSerializer(context.GetSerializationContextProvider())
                .SerializeToString(pomonaResponse.Entity,
                                   new SerializeOptions
                                   {
                                       ExpandedPaths = pomonaResponse.ExpandedPaths,
                                       ExpectedBaseType = pomonaResponse.ResultType
                                   });

            if (IsTextHtmlContentType(requestedMediaRange))
            {
                // Wrap in html
                var response = new Response();
                var htmlLinks = GetHtmlLinks();

                HtmlJsonPrettifier.CreatePrettifiedHtmlJsonResponse(response,
                                                                    htmlLinks,
                                                                    jsonString,
                                                                    "http://failfailtodo");
                return response;
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(jsonString);
                var response = new Response
                {
                    //Headers = {{"Content-Length", bytes.Length.ToString()}},
                    Contents = s => s.Write(bytes, 0, bytes.Length),
                    ContentType = ContentType,
                    StatusCode = pomonaResponse.StatusCode
                };

                if (pomonaResponse.ResponseHeaders != null)
                {
                    foreach (var kvp in pomonaResponse.ResponseHeaders)
                        response.Headers.Add(kvp);
                }

                // Add etag header
                var transformedResultType = pomonaResponse.ResultType as TransformedType;
                if (transformedResultType == null)
                    return response;

                var etagProperty = transformedResultType.ETagProperty;
                if (pomonaResponse.Entity == null || etagProperty == null)
                    return response;

                var etagValue = (string)etagProperty.GetValue(pomonaResponse.Entity);
                if (etagValue != null)
                {
                    // I think defining this as a weak etag will be correct, since we can specify $expand which change data (byte-by-byte).
                    response.Headers["ETag"] = String.Format("W/\"{0}\"", etagValue);
                }

                return response;
            }
        }


        protected abstract ITextSerializerFactory GetSerializerFactory(NancyContext context);


        protected bool IsTextHtmlContentType(MediaRange requestedMediaRange)
        {
            return requestedMediaRange.Matches("text/html");
        }


        private string GetHtmlLinks()
        {
            var routeCache = this.routeCacheProvider.GetCache();
            if (routeCache == null)
                return String.Empty;

            StringBuilder linkBuilder = new StringBuilder();

            var routes = routeCache
                .Select(r => r.Value)
                .SelectMany(r => r.Select(t => t.Item2));

            foreach (var route in routes)
            {
                if (!route.Metadata.Has<PomonaRouteMetadata>())
                    continue;

                var metadata = route.Metadata.Retrieve<PomonaRouteMetadata>();
                var rel = String.Concat("http://pomona.io/rel/", metadata.Relation);
                var contentType = metadata.ContentType;
                var methods = metadata.Method.ToString().ToUpperInvariant();
                var href = route.Path;

                linkBuilder.AppendFormat("<link rel=\"{0}\" type=\"{1}\" methods=\"{2}\" href=\"{3}\">{4}",
                                         rel,
                                         contentType,
                                         methods,
                                         href,
                                         Environment.NewLine);
            }

            return linkBuilder.ToString();
        }
    }
}