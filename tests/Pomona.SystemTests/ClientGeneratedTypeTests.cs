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

using System.Linq;
using Critters.Client;
using NUnit.Framework;

namespace Pomona.SystemTests
{
    [TestFixture]
    public class ClientGeneratedTypeTests
    {
        [Test]
        public void GeneratedPocoTypeInitializesDictionaryPropertyInConstructor()
        {
            var dictContainer = new DictionaryContainerResource();
            Assert.That(dictContainer.Map, Is.Not.Null);
        }

        [Test]
        public void GeneratedPocoTypeInitializesListPropertyInConstructor()
        {
            var critter = new CritterResource();
            Assert.That(critter.Weapons, Is.Not.Null);
        }

        [Test]
        public void GeneratedPocoTypeInitializesValueObjectPropertyInConstructor()
        {
            var critter = new CritterResource();
            Assert.That(critter.CrazyValue, Is.Not.Null);
        }

        [Test]
        public void MiddleBaseClassExcludedFromMapping_WillBeExcludedInGeneratedClient()
        {
            Assert.That(typeof (IInheritsFromHiddenBase).GetInterfaces(), Has.Member(typeof (IEntityBase)));
            Assert.That(typeof (Client).Assembly.GetTypes().Count(x => x.Name == "IHiddenBaseInMiddle"), Is.EqualTo(0));
            Assert.That(typeof (IInheritsFromHiddenBase).GetProperty("ExposedFromDerivedResource"), Is.Not.Null);
        }

        [Test]
        public void ThingIndependentFromBase_DoesNotInheritEntityBase()
        {
            Assert.That(!typeof (IEntityBase).IsAssignableFrom(typeof (IThingIndependentFromBase)));
        }
    }
}