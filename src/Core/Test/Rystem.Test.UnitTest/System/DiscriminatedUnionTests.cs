using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Rystem.Test.UnitTest.System
{
    public class DiscriminatedUnionTests
    {
        [Fact]
        public void SerializeAndDeserialize()
        {

            var testClass = new CurrentTestClass
            {
                OneClass_String = new FirstClass { FirstProperty = "OneClass_String.FirstProperty", SecondProperty = "OneClass_String.SecondProperty" },
                SecondClass_OneClass = new SecondClass
                {
                    FirstProperty = "SecondClass_OneClass.FirstProperty",
                    SecondProperty = "SecondClass_OneClass.SecondProperty"
                },
                OneClass_string__2 = "OneClass_string__2.string",
                Bool_Int = 3,
                Decimal_Bool = true,
                OneCLass_SecondClass_Int = 3,
                FirstClass_SecondClass_Int_ThirdClass = new ThirdClass
                {
                    Stringable = "ThirdClass.Stringable",
                    SecondClass = new SecondClass { SecondProperty = "FirstClass_SecondClass_Int_ThirdClass.SecondClass.SecondProperty", FirstProperty = "FirstClass_SecondClass_Int_ThirdClass.SecondClass.FirstProperty" },
                    ListOfSecondClasses = [new SecondClass { SecondProperty = "ListOfSecondClasses.SecondClass.SecondProperty[0]", FirstProperty = "ListOfSecondClasses.SecondClass.FirstProperty[0]" }],
                    DictionaryItems = new Dictionary<string, string> { ["key"] = "FirstClass_SecondClass_Int_ThirdClass.DictionaryItems.key", ["key2"] = "FirstClass_SecondClass_Int_ThirdClass.DictionaryItems.key2" },
                    ArrayOfStrings = ["FirstClass_SecondClass_Int_ThirdClass.ArrayOfStrings[0]", "FirstClass_SecondClass_Int_ThirdClass.ArrayOfStrings[1]", "FirstClass_SecondClass_Int_ThirdClass.ArrayOfStrings[2]"],
                    ObjectDictionary = new Dictionary<string, SecondClass>
                    {
                        ["key"] = new SecondClass { FirstProperty = "FirstClass_SecondClass_Int_ThirdClass.ObjectDictionary.FirstProperty.key", SecondProperty = "FirstClass_SecondClass_Int_ThirdClass.ObjectDictionary.SecondProperty.key" },
                        ["key2"] = new SecondClass { FirstProperty = "FirstClass_SecondClass_Int_ThirdClass.ObjectDictionary.FirstProperty.key2", SecondProperty = "FirstClass_SecondClass_Int_ThirdClass.ObjectDictionary.SecondProperty.key2" },
                    }
                }
            };
            var json = testClass.ToJson();
            var deserialized = json.FromJson<CurrentTestClass>();
            Assert.Equal(testClass.OneClass_String.AsT0!.FirstProperty, deserialized.OneClass_String!.AsT0!.FirstProperty);
            Assert.Equal(testClass.OneClass_String.AsT0!.SecondProperty, deserialized.OneClass_String!.AsT0!.SecondProperty);
            Assert.Equal(testClass.SecondClass_OneClass.AsT0!.FirstProperty, deserialized.SecondClass_OneClass!.AsT0!.FirstProperty);
            Assert.Equal(testClass.SecondClass_OneClass.AsT0!.SecondProperty, deserialized.SecondClass_OneClass!.AsT0!.SecondProperty);
            Assert.Equal(testClass.OneClass_string__2.AsT1, deserialized.OneClass_string__2!.AsT1);
            Assert.Equal(testClass.Bool_Int.AsT1, deserialized.Bool_Int!.AsT1);
            Assert.Equal(testClass.Decimal_Bool.AsT1, deserialized.Decimal_Bool!.AsT1);
            Assert.Equal(testClass.OneCLass_SecondClass_Int.AsT2, deserialized.OneCLass_SecondClass_Int!.AsT2);
            Assert.Equal(testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3!.ArrayOfStrings!.Count, deserialized.FirstClass_SecondClass_Int_ThirdClass!.AsT3!.ArrayOfStrings!.Count);
            Assert.Equal(testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3!.DictionaryItems!.Count, deserialized.FirstClass_SecondClass_Int_ThirdClass!.AsT3!.DictionaryItems!.Count);
            Assert.Equal(testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3!.ObjectDictionary!.Count, deserialized.FirstClass_SecondClass_Int_ThirdClass!.AsT3!.ObjectDictionary!.Count);
            Assert.Equal(testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3!.ListOfSecondClasses!.Count, deserialized.FirstClass_SecondClass_Int_ThirdClass!.AsT3!.ListOfSecondClasses!.Count);
            Assert.Equal(testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3!.Stringable!.Length, deserialized.FirstClass_SecondClass_Int_ThirdClass!.AsT3!.Stringable!.Length);
            foreach (var item in testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3!.ArrayOfStrings)
                Assert.Contains(item, deserialized.FirstClass_SecondClass_Int_ThirdClass.AsT3.ArrayOfStrings);
            foreach (var item in testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3.ListOfSecondClasses!)
            {
                Assert.Contains(deserialized.FirstClass_SecondClass_Int_ThirdClass.AsT3.ListOfSecondClasses!, t => t.FirstProperty == item.FirstProperty && t.SecondProperty == item.SecondProperty);
            }
            foreach (var item in testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3.DictionaryItems)
            {
                Assert.Contains(deserialized.FirstClass_SecondClass_Int_ThirdClass.AsT3.DictionaryItems, t => t.Key == item.Key && t.Value == item.Value);
            }
            foreach (var item in testClass.FirstClass_SecondClass_Int_ThirdClass.AsT3.ObjectDictionary)
            {
                Assert.Contains(deserialized.FirstClass_SecondClass_Int_ThirdClass.AsT3.ObjectDictionary, t => t.Key == item.Key && t.Value.FirstProperty == item.Value.FirstProperty && t.Value.SecondProperty == item.Value.SecondProperty);
            }
            Assert.Equal(json, deserialized.ToJson());
        }
        [Fact]
        public void ChangeValuesAndSerializeAndDeserialize()
        {

            var testClass = new CurrentTestClass
            {
                OneClass_String = new FirstClass { FirstProperty = "OneClass_String.FirstProperty", SecondProperty = "OneClass_String.SecondProperty" },
                SecondClass_OneClass = new SecondClass
                {
                    FirstProperty = "SecondClass_OneClass.FirstProperty",
                    SecondProperty = "SecondClass_OneClass.SecondProperty"
                },
                OneClass_string__2 = "OneClass_string__2.string",
                Bool_Int = 3,
                Decimal_Bool = true,
                OneCLass_SecondClass_Int = 3,
                FirstClass_SecondClass_Int_ThirdClass = new ThirdClass
                {
                    Stringable = "ThirdClass.Stringable",
                    SecondClass = new SecondClass { SecondProperty = "FirstClass_SecondClass_Int_ThirdClass.SecondClass.SecondProperty", FirstProperty = "FirstClass_SecondClass_Int_ThirdClass.SecondClass.FirstProperty" },
                    ListOfSecondClasses = [new SecondClass { SecondProperty = "ListOfSecondClasses.SecondClass.SecondProperty[0]", FirstProperty = "ListOfSecondClasses.SecondClass.FirstProperty[0]" }],
                    DictionaryItems = new Dictionary<string, string> { ["key"] = "FirstClass_SecondClass_Int_ThirdClass.DictionaryItems.key", ["key2"] = "FirstClass_SecondClass_Int_ThirdClass.DictionaryItems.key2" },
                    ArrayOfStrings = ["FirstClass_SecondClass_Int_ThirdClass.ArrayOfStrings[0]", "FirstClass_SecondClass_Int_ThirdClass.ArrayOfStrings[1]", "FirstClass_SecondClass_Int_ThirdClass.ArrayOfStrings[2]"],
                    ObjectDictionary = new Dictionary<string, SecondClass>
                    {
                        ["key"] = new SecondClass { FirstProperty = "FirstClass_SecondClass_Int_ThirdClass.ObjectDictionary.FirstProperty.key", SecondProperty = "FirstClass_SecondClass_Int_ThirdClass.ObjectDictionary.SecondProperty.key" },
                        ["key2"] = new SecondClass { FirstProperty = "FirstClass_SecondClass_Int_ThirdClass.ObjectDictionary.FirstProperty.key2", SecondProperty = "FirstClass_SecondClass_Int_ThirdClass.ObjectDictionary.SecondProperty.key2" },
                    }
                }
            };
            testClass.FirstClass_SecondClass_Int_ThirdClass = 3;
            var json = testClass.ToJson();
            var deserialized = json.FromJson<CurrentTestClass>();
            Assert.Equal(testClass.OneClass_String.AsT0!.FirstProperty, deserialized.OneClass_String!.AsT0!.FirstProperty);
            Assert.Equal(testClass.OneClass_String.AsT0!.SecondProperty, deserialized.OneClass_String!.AsT0!.SecondProperty);
            Assert.Equal(testClass.SecondClass_OneClass.AsT0!.FirstProperty, deserialized.SecondClass_OneClass!.AsT0!.FirstProperty);
            Assert.Equal(testClass.SecondClass_OneClass.AsT0!.SecondProperty, deserialized.SecondClass_OneClass!.AsT0!.SecondProperty);
            Assert.Equal(testClass.OneClass_string__2.AsT1, deserialized.OneClass_string__2!.AsT1);
            Assert.Equal(testClass.Bool_Int.AsT1, deserialized.Bool_Int!.AsT1);
            Assert.Equal(testClass.Decimal_Bool.AsT1, deserialized.Decimal_Bool!.AsT1);
            Assert.Equal(testClass.OneCLass_SecondClass_Int.AsT2, deserialized.OneCLass_SecondClass_Int!.AsT2);
            Assert.Equal(testClass.FirstClass_SecondClass_Int_ThirdClass.CastT2, deserialized.FirstClass_SecondClass_Int_ThirdClass!.CastT2);
            Assert.Equal(json, deserialized.ToJson());
        }
        [Fact]
        public void DeserializationSignature()
        {
            var testClass = new SignatureTestClass
            {
                Test = new SignatureClassTwo
                {
                    FirstProperty = "FirstProperty",
                    SecondProperty = "SecondProperty"
                }
            };
            var json = testClass.ToJson();
            var deserialized = json.FromJson<SignatureTestClass>();
            //It's correct that the class during the deserialization is not the same as the original one, because the deserialization is based on the name of the properties (signature method), and both classes are the same in terms of properties.
            Assert.True(deserialized.Test!.Is<SignatureClassOne>(out var test));
            Assert.False(deserialized.Test!.Is<SignatureClassTwo>(out var test2));
            Assert.NotNull(test);
            Assert.Null(test2);
        }
        [Fact]
        public void DeserializeRequiredProperties()
        {
            var json = """
                {
                  "id": "asst_wGCsr359S5tbUQYT9SJ9VYpk",
                  "object": "assistant",
                  "created_at": 1734858272,
                  "name": null,
                  "description": null,
                  "model": "gpt-4o",
                  "instructions": "You are a personal math tutor. When asked a question, write and run Python code to answer the question.",
                  "tools": [
                    {
                      "type": "code_interpreter"
                    }
                  ],
                  "top_p": 1.0,
                  "temperature": 0.5,
                  "tool_resources": {
                    "code_interpreter": {
                      "file_ids": []
                    }
                  },
                  "metadata": {},
                  "response_format": "auto"
                }
                """;
            var deserialized = json.FromJson<AssistantRequest>();
            Assert.Equal("asst_wGCsr359S5tbUQYT9SJ9VYpk", deserialized.Id);
            Assert.Equal(1.0, deserialized.TopP);
            Assert.Equal(0.5, deserialized.Temperature);
            Assert.Equal("auto", deserialized.ResponseFormat);
        }
        [Fact]
        public void RightChoiceWithAttribute()
        {
            var testClass = new ChosenClass
            {
                FirstProperty = new TheSecondChoice(),
                SecondProperty = "SecondProperty"
            };
            var json = testClass.ToJson();
            var deserialized = json.FromJson<ChosenClass>();
            Assert.True(deserialized.FirstProperty!.Is<TheSecondChoice>());
        }
        private sealed class ChosenClass
        {
            public AnyOf<TheFirstChoice, TheSecondChoice>? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        public sealed class TheFirstChoice
        {
            [JsonAnyOfChooser("first")]
            public string Type { get; } = "first";
        }
        public sealed class TheSecondChoice
        {
            [JsonAnyOfChooser("second")]
            public string Type { get; } = "second";
        }
        private sealed class SignatureTestClass
        {
            public AnyOf<SignatureClassOne, SignatureClassTwo>? Test { get; set; }
        }
        private sealed class SignatureClassOne
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        private sealed class SignatureClassTwo
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        private sealed class CurrentTestClass
        {
            [JsonPropertyName("c")]
            public AnyOf<SecondClass, FirstClass>? SecondClass_OneClass { get; set; }
            [JsonPropertyName("m")]
            public AnyOf<FirstClass, string>? OneClass_String { get; set; }
            [JsonPropertyName("f")]
            public AnyOf<FirstClass, string>? OneClass_string__2 { get; set; }
            [JsonPropertyName("s")]
            public AnyOf<bool, int>? Bool_Int { get; set; }
            [JsonPropertyName("p")]
            public AnyOf<decimal, bool>? Decimal_Bool { get; set; }
            public AnyOf<string, int>? Test { get; set; }
            [JsonPropertyName("d")]
            public AnyOf<FirstClass, SecondClass, int>? OneCLass_SecondClass_Int { get; set; }
            [JsonPropertyName("e")]
            public AnyOf<FirstClass, SecondClass, int, ThirdClass>? FirstClass_SecondClass_Int_ThirdClass { get; set; }
        }
        private sealed class FirstClass
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        private sealed class SecondClass
        {
            [JsonPropertyName("FirstProperty")]
            public string? FirstProperty { get; set; }
            [JsonPropertyName("dd")]
            public string? SecondProperty { get; set; }
        }
        private sealed class ThirdClass
        {
            public string? Stringable { get; set; }
            public SecondClass? SecondClass { get; set; }
            [JsonPropertyName("array")]
            public List<string>? ArrayOfStrings { get; set; }
            public List<SecondClass>? ListOfSecondClasses { get; set; }
            public Dictionary<string, string>? DictionaryItems { get; set; }
            [JsonPropertyName("a")]
            public Dictionary<string, SecondClass>? ObjectDictionary { get; set; }
        }
        private sealed class AssistantRequest
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("object")]
            public string? Object { get; set; }
            [JsonPropertyName("created_at")]
            public long? CreatedAt { get; set; }
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            [JsonPropertyName("description")]
            public string? Description { get; set; }
            [JsonPropertyName("model")]
            public string? Model { get; set; }
            [JsonIgnore]
            public StringBuilder? InstructionsBuilder { get; set; }
            [JsonPropertyName("instructions")]
            public string? Instructions { get => InstructionsBuilder?.ToString(); set => InstructionsBuilder = new(value); }
            [JsonPropertyName("metadata")]
            public Dictionary<string, string>? Metadata { get; set; }
            [JsonPropertyName("response_format")]
            public AnyOf<string, ResponseFormatChatRequest>? ResponseFormat { get; set; }
            [JsonPropertyName("temperature")]
            public double? Temperature { get; set; }
            [JsonPropertyName("top_p")]
            public double? TopP { get; set; }
            [JsonPropertyName("tools")]
            public List<AnyOf<AssistantFunctionTool, AssistantCodeInterpreterTool, AssistantFileSearchTool>>? Tools { get; set; }
            [JsonPropertyName("tool_resources")]
            public AssistantToolResources? ToolResources { get; set; }
        }
        public sealed class AssistantToolResources
        {
            [JsonPropertyName("code_interpreter")]
            public AssistantCodeInterpreterTool? CodeInterpreter { get; set; }
            [JsonPropertyName("file_search")]
            public AssistantFileSearchToolResources? FileSearch { get; set; }
        }

        public sealed class AssistantCodeInterpreterToolResources
        {
            [JsonPropertyName("file_ids")]
            public List<string>? Files { get; set; }
        }
        public sealed class AssistantStaticChunkingStrategyVectorStoresFileSearchToolResources
        {
            [JsonPropertyName("max_chunk_size_tokens")]
            public int MaxChunkSizeTokens { get; set; } = 800;
            [JsonPropertyName("chunk_overlap_tokens")]
            public int ChunkOverlapTokens { get; set; } = 400;
        }
        public sealed class AssistantChunkingStrategyVectorStoresFileSearchToolResources
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("static")]
            public AssistantStaticChunkingStrategyVectorStoresFileSearchToolResources? Static { get; set; }
        }
        public sealed class AssistantVectorStoresFileSearchToolResources
        {
            [JsonPropertyName("file_ids")]
            public List<string>? Files { get; set; }
            [JsonPropertyName("chunking_strategy")]
            public AssistantChunkingStrategyVectorStoresFileSearchToolResources? ChunkingStrategy { get; set; }
        }
        public sealed class AssistantFileSearchToolResources
        {
            [JsonPropertyName("vector_store_ids")]
            public List<string>? VectorStoresId { get; set; }
            [JsonPropertyName("vector_stores")]
            public AssistantVectorStoresFileSearchToolResources? VectorStores { get; set; }
            [JsonPropertyName("metadata")]
            public Dictionary<string, string>? Metadata { get; set; }
        }
        public sealed class AssistantCodeInterpreterTool
        {
            private const string FileType = "code_interpreter";
            [JsonPropertyName("type")]
            public string Type { get; } = FileType;
        }
        public sealed class AssistantFileSearchTool
        {
            private const string FileType = "file_search";
            [JsonPropertyName("type")]
            public string Type { get; } = FileType;
            [JsonPropertyName("file_search")]
            public AssistantSettingsForFileSearchTool? FileSearch { get; set; }
        }
        public sealed class AssistantSettingsForFileSearchTool
        {
            [JsonPropertyName("max_num_results")]
            public int MaxNumberOfResults { get; set; }
            [JsonPropertyName("ranking_options")]
            public AssistantRankerSettingsForFileSearchTool? RankingOptions { get; set; }
        }
        public sealed class AssistantRankerSettingsForFileSearchTool
        {
            [JsonPropertyName("ranker")]
            public string? Ranker { get; set; }
            [JsonPropertyName("score_threshold")]
            public int ScoreThreshold { get; set; }
        }
        public sealed class AssistantFunctionTool
        {
            private const string FunctionType = "function";
            [JsonPropertyName("type")]
            public string Type { get; } = FunctionType;
            [JsonPropertyName("function")]
            public FunctionTool? Function { get; set; }
        }
        public sealed class ResponseFormatChatRequest
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("content")]
            public FunctionTool? Content { get; set; }
        }
        public sealed class FunctionTool
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;
            [JsonPropertyName("description")]
            public string Description { get; set; } = null!;
            [JsonPropertyName("parameters")]
            public FunctionToolMainProperty Parameters { get; set; } = null!;
            [JsonPropertyName("strict")]
            public bool? Strict { get; set; }
        }
        public class FunctionToolMainProperty : FunctionToolNonPrimitiveProperty
        {
            public FunctionToolMainProperty() : base()
            {
            }
            [JsonPropertyName("required")]
            public List<string>? RequiredParameters { get; set; }
            [JsonPropertyName("additionalProperties")]
            public bool AdditionalProperties => _numberOfProperties != (RequiredParameters?.Count ?? 0);
        }
        public class FunctionToolNonPrimitiveProperty : FunctionToolProperty
        {
            internal int _numberOfProperties;
            internal const string DefaultTypeName = "object";
            public FunctionToolNonPrimitiveProperty()
            {
                Type = DefaultTypeName;
                Properties = [];
            }
            [JsonPropertyName("properties")]
            public Dictionary<string, FunctionToolProperty> Properties { get; }
        }
        [JsonDerivedType(typeof(FunctionToolEnumProperty))]
        [JsonDerivedType(typeof(FunctionToolNumberProperty))]
        [JsonDerivedType(typeof(FunctionToolNonPrimitiveProperty))]
        [JsonDerivedType(typeof(FunctionToolArrayProperty))]
        [JsonDerivedType(typeof(FunctionToolPrimitiveProperty))]
        public abstract class FunctionToolProperty
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }
            private const string DefaultTypeName = "string";
            public FunctionToolProperty()
            {
                Type = DefaultTypeName;
            }
        }
        public sealed class FunctionToolEnumProperty : FunctionToolProperty
        {
            private const string DefaultTypeName = "string";
            public FunctionToolEnumProperty()
            {
                Type = DefaultTypeName;
            }
            [JsonPropertyName("enum")]
            public List<string>? Enums { get; set; }
        }
        public sealed class FunctionToolNumberProperty : FunctionToolProperty
        {
            private const string DefaultTypeName = "number";
            public FunctionToolNumberProperty()
            {
                Type = DefaultTypeName;
            }
            [JsonPropertyName("multipleOf")]
            public double? MultipleOf { get; set; }
            [JsonPropertyName("minimum")]
            public double? Minimum { get; set; }
            [JsonPropertyName("maximum")]
            public double? Maximum { get; set; }
            [JsonPropertyName("exclusiveMinimum")]
            public bool? ExclusiveMinimum { get; set; }
            [JsonPropertyName("exclusiveMaximum")]
            public bool? ExclusiveMaximum { get; set; }
            [JsonPropertyName("description")]
            public string? Description { get; set; }
        }
        public sealed class FunctionToolArrayProperty : FunctionToolProperty
        {
            [JsonPropertyName("items")]
            public FunctionToolProperty? Items { get; set; }
            [JsonPropertyName("description")]
            public string? Description { get; set; }
        }
        public sealed class FunctionToolPrimitiveProperty : FunctionToolProperty
        {
            [JsonPropertyName("description")]
            public string? Description { get; set; }
            public FunctionToolPrimitiveProperty() : base()
            {
            }
        }
    }
}
