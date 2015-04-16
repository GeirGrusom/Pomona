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

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;

using Pomona.Common;
using Pomona.Common.TypeSystem;
using Pomona.Documentation.Nodes;
using Pomona.Documentation.Xml;
using Pomona.Documentation.Xml.Serialization;

namespace Pomona.Documentation
{
    public class XmlDocumentationProvider : IDocumentationProvider
    {
        private readonly Dictionary<string, XDoc> xmlDocs = new Dictionary<string, XDoc>();
        private readonly XmlDocMapper mapper;


        public XmlDocumentationProvider(IResourceTypeResolver typeMapper)
        {
            this.mapper = new XmlDocMapper(typeMapper);
        }


        private IDocNode GetMemberSummary(MemberInfo member)
        {
            var xdoc = this.xmlDocs.GetOrCreate(member.Module.Assembly.FullName, () => LoadXmlDoc(member));
            if (xdoc == null)
                return null;
            var xDocContentContainer = xdoc.GetSummary(member);
            if (xDocContentContainer == null)
                return null;
            return mapper.Map(xDocContentContainer);
        }


        private static XDoc LoadXmlDoc(MemberInfo member)
        {
            var xmlDocFileName = member.Module.Assembly.GetName().Name + ".xml";
            if (File.Exists(xmlDocFileName))
            {
                using (var stream = File.OpenRead(xmlDocFileName))
                {
                    return new XDoc(XDocument.Load(stream).Root);
                }
            }
            return null;
        }


        public IDocNode GetSummary(MemberSpec member)
        {
            return GetMemberSummary(member.Member);
        }
    }
}