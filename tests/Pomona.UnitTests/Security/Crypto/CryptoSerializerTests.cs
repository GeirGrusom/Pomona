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
using System.Runtime.Serialization;
using System.Security.Cryptography;

using NUnit.Framework;

using Pomona.Security.Authentication;
using Pomona.Security.Crypto;

namespace Pomona.UnitTests.Security.Crypto
{
    [TestFixture]
    public class CryptoSerializerTests
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            this.serializer = new TestCryptoSerializer();
        }

        #endregion

        public class TestCryptoSerializer : CryptoSerializerBase
        {
            private readonly Func<SymmetricAlgorithm> algoFactory;


            public TestCryptoSerializer(Func<SymmetricAlgorithm> algoFactory = null)
                : base(new FixedSiteKeyProvider(), new NonRandomGenerator())
            {
                this.algoFactory = algoFactory;
            }


            protected override SymmetricAlgorithm CreateSymmetricalAlgorithm()
            {
                return this.algoFactory != null ? this.algoFactory() : base.CreateSymmetricalAlgorithm();
            }
        }

        private CryptoSerializerBase serializer;

        public class FixedSiteKeyProvider : ISiteKeyProvider
        {
            private static readonly byte[] key =
            {
                0xa2, 0xaf, 0x89, 0x23, 0x75, 0x06, 0xc2, 0x62, 0xc8, 0xef, 0x37, 0xcb, 0xa0, 0x01, 0xae, 0xdf,
                0x3a, 0xef, 0x27, 0x2b, 0xcb, 0x8a, 0xc5, 0xe7, 0x8d, 0xa2, 0x6e, 0xeb, 0x59, 0x76, 0xeb, 0x0e
            };

            public byte[] SiteKey
            {
                get { return key; }
            }
        }

        public class NonRandomGenerator : RandomNumberGenerator
        {
            public override void GetBytes(byte[] data)
            {
                for (var i = 0; i < data.Length; i++)
                    data[i] = (byte)i;
            }


            public override void GetNonZeroBytes(byte[] data)
            {
                throw new NotImplementedException();
            }
        }

        public class TestClass
        {
            public string Bar { get; set; }
            public string Foo { get; set; }
        }


        [TestCase("krakra", "go to gate")]
        [TestCase("this string is quite a bit longer sorry about that maybe you should consider shortening this down",
            "djhskj")]
        [TestCase("abcd", "ehfkdjfklsdfjklsdjfl")]
        public void SerializeThenDeserialize_ReturnsCorrectValues(string fooValue, string barValue)
        {
            var obj = new TestClass() { Foo = fooValue, Bar = barValue };
            var serialized = this.serializer.Serialize(obj);
            var deserialized = this.serializer.Deserialize<TestClass>(serialized);
            Console.WriteLine("Serialized: " + serialized);
            Assert.That(deserialized.Foo, Is.EqualTo(fooValue));
            Assert.That(deserialized.Bar, Is.EqualTo(barValue));
        }


        [Test]
        public void DeserializeRandomNonRecognizedBytes_ThrowsSerializationException()
        {
            Assert.Throws<SerializationException>(
                () =>
                    this.serializer.Deserialize<TestClass>(
                        "AAECAwQFBgcICQoLDA0OD7qVKYq08JRY-62b0QSPIt_MhNreTSJVIkGRLqQz5uuSG3w."));
        }


        [Test]
        public void Deserialize_WithNullArg_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => this.serializer.Deserialize<TestClass>(null));
        }
    }
}