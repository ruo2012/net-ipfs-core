using Ipfs.Registry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Google.Protobuf;

namespace Ipfs
{
    [TestClass]
    public class MultiHashTest
    {
        [TestMethod]
        public void HashNames()
        {
            var mh = new MultiHash("sha1", new byte[20]);
            mh = new MultiHash("sha2-256", new byte[32]);
            mh = new MultiHash("sha2-512", new byte[64]);
            mh = new MultiHash("keccak-512", new byte[64]);
        }

        [TestMethod]
        public void Unknown_Hash_Name()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new MultiHash(null, new byte[0]));
            ExceptionAssert.Throws<ArgumentException>(() => new MultiHash("", new byte[0]));
            ExceptionAssert.Throws<ArgumentException>(() => new MultiHash("md5", new byte[0]));
        }

        [TestMethod]
        public void Write_Null_Stream()
        {
            var mh = new MultiHash("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB");
            ExceptionAssert.Throws<ArgumentNullException>(() => mh.Write((CodedOutputStream)null));
        }

        [TestMethod]
        public void Parsing_Unknown_Hash_Number()
        {
            HashingAlgorithm unknown = null;
            EventHandler<UnknownHashingAlgorithmEventArgs> unknownHandler = (s, e) => { unknown = e.Algorithm; };
            var ms = new MemoryStream(new byte[] { 0x01, 0x02, 0x0a, 0x0b });
            MultiHash.UnknownHashingAlgorithm += unknownHandler;
            try
            {
                var mh = new MultiHash(ms);
                Assert.AreEqual("ipfs-1", mh.Algorithm.Name);
                Assert.AreEqual("ipfs-1", mh.Algorithm.ToString());
                Assert.AreEqual(1, mh.Algorithm.Code);
                Assert.AreEqual(2, mh.Algorithm.DigestSize);
                Assert.AreEqual(0xa, mh.Digest[0]);
                Assert.AreEqual(0xb, mh.Digest[1]);
                Assert.IsNotNull(unknown, "unknown handler not called");
                Assert.AreEqual("ipfs-1", unknown.Name);
                Assert.AreEqual(1, unknown.Code);
                Assert.AreEqual(2, unknown.DigestSize);
            }
            finally
            {
                MultiHash.UnknownHashingAlgorithm -= unknownHandler;
            }
        }

        [TestMethod]
        public void Parsing_Wrong_Digest_Size()
        {
            var ms = new MemoryStream(new byte[] { 0x11, 0x02, 0x0a, 0x0b });
            ExceptionAssert.Throws<InvalidDataException>(() => new MultiHash(ms));
        }

        [TestMethod]
        public void Invalid_Digest()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new MultiHash("sha1", null));
            ExceptionAssert.Throws<ArgumentException>(() => new MultiHash("sha1", new byte[0]));
            ExceptionAssert.Throws<ArgumentException>(() => new MultiHash("sha1", new byte[21]));
        }

        [TestMethod]
        public void Base58_Encode_Decode()
        {
            var mh = new MultiHash("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB");
            Assert.AreEqual("sha2-256", mh.Algorithm.Name);
            Assert.AreEqual(32, mh.Digest.Length);
            Assert.AreEqual("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB", mh.ToBase58());
        }

        [TestMethod]
        public void Compute_Hash_Array()
        {
            var hello = Encoding.UTF8.GetBytes("Hello, world.");
            var mh = MultiHash.ComputeHash(hello);
            Assert.AreEqual(MultiHash.DefaultAlgorithmName, mh.Algorithm.Name);
            Assert.IsNotNull(mh.Digest);
        }

        [TestMethod]
        public void Compute_Hash_Stream()
        {
            var hello = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world."));
            hello.Position = 0;
            var mh = MultiHash.ComputeHash(hello);
            Assert.AreEqual(MultiHash.DefaultAlgorithmName, mh.Algorithm.Name);
            Assert.IsNotNull(mh.Digest);
        }

        [TestMethod]
        public void Compute_Not_Implemented_Hash_Array()
        {
            var alg = HashingAlgorithm.Register("not-implemented", 0x0F, 32);
            try
            {
                var hello = Encoding.UTF8.GetBytes("Hello, world.");
                ExceptionAssert.Throws<NotImplementedException>(() => MultiHash.ComputeHash(hello, "not-implemented"));
            }
            finally
            {
                HashingAlgorithm.Deregister(alg);
            }
        }

        [TestMethod]
        public void Matches_Array()
        {
            var hello = Encoding.UTF8.GetBytes("Hello, world.");
            var hello1 = Encoding.UTF8.GetBytes("Hello, world");
            var mh = MultiHash.ComputeHash(hello);
            Assert.IsTrue(mh.Matches(hello));
            Assert.IsFalse(mh.Matches(hello1));

            var mh1 = MultiHash.ComputeHash(hello, "sha1");
            Assert.IsTrue(mh1.Matches(hello));
            Assert.IsFalse(mh1.Matches(hello1));

            var mh2 = MultiHash.ComputeHash(hello, "sha2-512");
            Assert.IsTrue(mh2.Matches(hello));
            Assert.IsFalse(mh2.Matches(hello1));

            var mh3 = MultiHash.ComputeHash(hello, "keccak-512");
            Assert.IsTrue(mh3.Matches(hello));
            Assert.IsFalse(mh3.Matches(hello1));
        }

        [TestMethod]
        public void Matches_Stream()
        {
            var hello = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world."));
            var hello1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world"));
            hello.Position = 0;
            var mh = MultiHash.ComputeHash(hello);

            hello.Position = 0;
            Assert.IsTrue(mh.Matches(hello));

            hello1.Position = 0;
            Assert.IsFalse(mh.Matches(hello1));
        }

        [TestMethod]
        public void Wire_Formats()
        {
            var hashes = new[]
            {
                "5drNu81uhrFLRiS4bxWgAkpydaLUPW", // sha1
                "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4", // sha2_256
                "8Vtkv2tdQ43bNGdWN9vNx9GVS9wrbXHk4ZW8kmucPmaYJwwedXir52kti9wJhcik4HehyqgLrQ1hBuirviLhxgRBNv", // sha2_512
            };
            var helloWorld = Encoding.UTF8.GetBytes("hello world");
            foreach (var hash in hashes)
            {
                var mh = new MultiHash(hash);
                Assert.IsTrue(mh.Matches(helloWorld), hash);
            }
        }

        [TestMethod]
        public void To_String_Is_Base58_Representation()
        {
            var hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            var mh = new MultiHash(hash);
            Assert.AreEqual(hash, mh.ToString());
        }

        [TestMethod]
        public void Implicit_Conversion_From_String()
        {
            var hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            MultiHash mh = hash;
            Assert.IsNotNull(mh);
            Assert.IsInstanceOfType(mh, typeof(MultiHash));
            Assert.AreEqual(hash, mh.ToString());
        }

        [TestMethod]
        public void Value_Equality()
        {
            var a0 = new MultiHash("QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4");
            var a1 = new MultiHash("QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4");
            var b = new MultiHash("QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L5");
            MultiHash c = null;
            MultiHash d = null;

            Assert.IsTrue(c == d);
            Assert.IsFalse(c == b);
            Assert.IsFalse(b == c);

            Assert.IsFalse(c != d);
            Assert.IsTrue(c != b);
            Assert.IsTrue(b != c);

#pragma warning disable 1718
            Assert.IsTrue(a0 == a0);
            Assert.IsTrue(a0 == a1);
            Assert.IsFalse(a0 == b);

#pragma warning disable 1718
            Assert.IsFalse(a0 != a0);
            Assert.IsFalse(a0 != a1);
            Assert.IsTrue(a0 != b);

            Assert.IsTrue(a0.Equals(a0));
            Assert.IsTrue(a0.Equals(a1));
            Assert.IsFalse(a0.Equals(b));

            Assert.AreEqual(a0, a0);
            Assert.AreEqual(a0, a1);
            Assert.AreNotEqual(a0, b);

            Assert.AreEqual<MultiHash>(a0, a0);
            Assert.AreEqual<MultiHash>(a0, a1);
            Assert.AreNotEqual<MultiHash>(a0, b);

            Assert.AreEqual(a0.GetHashCode(), a0.GetHashCode());
            Assert.AreEqual(a0.GetHashCode(), a1.GetHashCode());
            Assert.AreNotEqual(a0.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void Varint_Hash_Code_and_Length()
        {
            var concise = "1220f8c3bf62a9aa3e6fc1619c250e48abe7519373d3edf41be62eb5dc45199af2ef"
                .ToHexBuffer();
            var mh = new MultiHash(new MemoryStream(concise, false));
            Assert.AreEqual("sha2-256", mh.Algorithm.Name);
            Assert.AreEqual(0x12, mh.Algorithm.Code);
            Assert.AreEqual(0x20, mh.Algorithm.DigestSize);

            var longer = "9200a000f8c3bf62a9aa3e6fc1619c250e48abe7519373d3edf41be62eb5dc45199af2ef"
                .ToHexBuffer();
            mh = new MultiHash(new MemoryStream(longer, false));
            Assert.AreEqual("sha2-256", mh.Algorithm.Name);
            Assert.AreEqual(0x12, mh.Algorithm.Code);
            Assert.AreEqual(0x20, mh.Algorithm.DigestSize);
        }

        [TestMethod]
        public void Compute_Hash_All_Algorithms()
        {
            foreach (var alg in HashingAlgorithm.All)
            {
                try
                {
                    var mh = MultiHash.ComputeHash(new byte[0], alg.Name);
                    Assert.IsNotNull(mh, alg.Name);
                    Assert.AreEqual(alg.Code, mh.Algorithm.Code, alg.Name);
                    Assert.AreEqual(alg.Name, mh.Algorithm.Name, alg.Name);
                    Assert.AreEqual(alg.DigestSize, alg.DigestSize, alg.Name);
                    Assert.AreEqual(alg.DigestSize, mh.Digest.Length, alg.Name);
                }
                catch (NotImplementedException)
                {
                    // If NYI then can't test it.
                }
            }
        }
    }
}
