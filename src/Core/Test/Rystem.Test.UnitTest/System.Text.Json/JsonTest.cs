using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Rystem.Test.UnitTest.Json
{
    public class JsonTest
    {
        internal sealed class JsonModel
        {
            public string? X { get; set; }
            public int Id { get; set; }
            public string? B { get; set; }
            public Guid E { get; set; }
            public bool Sol { get; set; }
        }
        private static readonly List<JsonModel> _models = new();
        static JsonTest()
        {
            for (int i = 0; i < 100; i++)
                _models.Add(new JsonModel { X = i.ToString(), Id = i, B = i.ToString(), E = Guid.NewGuid(), Sol = i % 2 == 0 });
        }
        [Fact]
        public void Test1()
        {
            var value = _models.ToJson();
            var models2 = value.FromJson<List<JsonModel>>();
            Assert.Equal(_models.Count, models2.Count);
        }
        [Fact]
        public void Test2()
        {
            var value = _models.ToJson();
            var models2 = value.FromJson(typeof(List<JsonModel>));
            bool check = models2 is List<JsonModel>;
            Assert.True(check);
        }
    }
}