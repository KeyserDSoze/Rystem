using System.Collections.Generic;
using System.Text.Json.Serialization;
using RepositoryFramework.ProgrammingLanguage;
using Xunit;

namespace RepositoryFramework.UnitTest.Unitary
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
            [JsonPropertyName("caramux")]
            public Dictionary<string, List<SomethingNew>> Formi { get; set; }
        }
        public class SomethingNew
        {
            [JsonPropertyName("baccano")]
            public string Bacca { get; set; }
            [JsonPropertyName("dalk")]
            public Solis Dalk { get; set; }
            [JsonPropertyName("rudo")]
            public Dictionary<string, List<SomethingNew>> Formi { get; set; }
        }
        public enum Solis
        {
            Alk = 3,
            Mold = 4,
            Salut = 56
        }
        public class SomethingNew2
        {
            [JsonPropertyName("baccano")]
            public string Bacca { get; set; }
        }
        [Fact]
        public void Test1()
        {
            var textCheck = """
                export type InModel = {
                alfi: string;
                bilus: Array<SomethingNew>;
                bilus34: Map<string, SomethingNew2>;
                caramux: Map<string, Array<SomethingNew>>;
                }
                export type SomethingNew = {
                baccano: string;
                dalk: Solis;
                rudo: Map<string, Array<SomethingNew>>;
                }
                export enum Solis {
                Alk = 3,
                Mold = 4,
                Salut = 56,
                }
                export type SomethingNew2 = {
                baccano: string;
                }
                """;
            var response = typeof(InModel).ConvertAs(ProgrammingLanguageType.Typescript);
            Assert.NotNull(response);
            Assert.Equal(response.Text, textCheck);
        }
    }
}
