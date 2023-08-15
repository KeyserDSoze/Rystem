using System.Collections.Generic;
using System.Text.Json.Serialization;
using RepositoryFramework;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class ProgrammingLanguageConverterTest
    {
        private sealed class InModel
        {
            [JsonPropertyName("alfi")]
            public string Alfi { get; set; }
            [JsonPropertyName("bilus")]
            public SomethingNew[] Bilus { get; set; }
            [JsonPropertyName("bilus34")]
            public Dictionary<string, SomethingNew2> Bilus3 { get; set; }
        }
        public class SomethingNew
        {
            [JsonPropertyName("baccano")]
            public string Bacca { get; set; }
        }
        public class SomethingNew2
        {
            [JsonPropertyName("baccano")]
            public string Bacca { get; set; }
        }
        [Fact]
        public void Test1()
        {
            //var response = typeof(InModel).ConvertAs(ProgrammingLanguage.Typescript);
            //Assert.NotNull(response);
        }
    }
}
