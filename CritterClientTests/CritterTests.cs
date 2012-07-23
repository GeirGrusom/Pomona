﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2012 Karsten Nikolai Strand
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
using System.Linq;

using CritterClient;

using NUnit.Framework;

using Nancy.Hosting.Self;

using Pomona.Example;

namespace CritterClientTests
{
    public class CritterModuleInternal : CritterModule
    {
    }

    /// <summary>
    /// Tests for generated assembly
    /// </summary>
    [TestFixture]
    public class CritterTests
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            this.baseUri = "http://localhost:4186/";
            this.host = new NancyHost(new Uri("http://localhost:4186"));
            this.host.Start();
        }


        [TearDown]
        public void TearDown()
        {
            this.host.Stop();
        }

        #endregion

        private NancyHost host;
        private string baseUri;


        [Test]
        public void DeserializeCritters()
        {
            var client = new ClientHelper();
            client.BaseUri = this.baseUri;
            var critters = client.List<Critter>("critter.weapons.model");
            var allSubscriptions = critters.SelectMany(x => x.Subscriptions).ToList();
        }
    }
}