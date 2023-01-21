using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;

namespace Rystem.Test.UnitTest.Security.Cryptography
{
    public class HashTest
    {
        public class Foo
        {
            public IEnumerable<string> Values { get; init; }
            public bool X { get; init; }
        }

        [Fact]
        public void Run()
        {
            var foo = new Foo()
            {
                Values = new List<string>() { "aa", "bb", "cc" },
                X = true
            };
            Assert.Equal(foo.ToHash(), foo.ToHash());
            var message = Guid.NewGuid();
            Assert.Equal(message.ToHash(), message.ToHash());
            var k = Guid.Parse("41e2c840-8ba1-4c0b-8a9b-781747a5de0c");
            var s = k.ToHash();
            var ss = k.ToHash().ToHash();
            Assert.Equal("18edf95916c3aa4fd09a754e2e799fce252b0b7a76ffff76962175ad0f9921bc13bbd675954c1121d9177ffc222622c5adecf8544acb7a844117d6b1fab4590a", k.ToHash());
            Assert.Equal("37116775626f2671434818e734c1172897cae679586e256b83266af408cd395d4c077c55c5173fa3f20d258c3e0df80d1f5f238dafc9c34e60dcd11d7bca11be", k.ToHash().ToHash());
        }
    }
}
