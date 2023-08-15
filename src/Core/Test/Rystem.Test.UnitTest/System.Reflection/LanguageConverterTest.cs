using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using RepositoryFramework.Api.Server.TypescriptModelsCreatorEngine;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class LanguageConverterTest
    {
        private sealed class InModel
        {
            [JsonPropertyName("alfi")]
            public string Alfi { get; set; }
            [JsonPropertyName("bilus")]
            public SomethingNew Bilus { get; set; }
        }
        public class SomethingNew
        {
            [JsonPropertyName("baccano")]
            public string Bacca { get; set; }
        }
        [Fact]
        public void Test1()
        {
            var response = new TypescriptModelCreatorEngine().Transform(null, typeof(InModel));
            Assert.NotNull(response);
        }
    }
}
