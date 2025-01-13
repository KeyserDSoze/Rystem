using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest.System
{
    public class DiscriminatedUnionTests
    {
        [Fact]
        public void NullCheckWithString()
        {
            string? value = null;
            if (value is string x)
            {
                Assert.Fail();
            }
            AnyOf<string?, NullCheckTest> anyOf = value;
            Assert.True(anyOf.IsT0);
            Assert.True(anyOf.TryGetT0(out _));
            Assert.False(anyOf.IsT1);
            Assert.Equal(0, anyOf.Index);
        }
        [Fact]
        public void NullCheckWithEnum()
        {
            NullCheckTest? value = null;
            if (value is NullCheckTest x)
            {
                Assert.Fail();
            }
            AnyOf<string?, NullCheckTest?> anyOf = value;
            Assert.True(anyOf.IsT1);
            Assert.True(anyOf.TryGetT1(out _));
            Assert.False(anyOf.IsT0);
            Assert.Equal(1, anyOf.Index);
        }
        public enum NullCheckTest
        {
            First,
            Second
        }
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
                  "id": "asst_FFvugfFeVLQzbnaTsF2Mvp31",
                  "object": "assistant",
                  "created_at": 1736092577,
                  "name": null,
                  "description": null,
                  "model": "gpt-4o-2",
                  "instructions": "You are a personal math tutor. When asked a question, write and run Python code to answer the question.",
                  "tools": [
                    {
                      "type": "code_interpreter"
                    },
                    {
                      "type": "file_search",
                      "file_search": {
                        "max_num_results": 20,
                        "ranking_options": {
                          "ranker": "default_2024_08_21",
                          "score_threshold": 0.0
                        }
                      }
                    }
                  ],
                  "top_p": 1.0,
                  "temperature": 0.5,
                  "tool_resources": {
                    "file_search": {
                      "vector_store_ids": [
                        "vs_ab4UC7phv8eirzQlbVrQW10T"
                      ]
                    },
                    "code_interpreter": {
                      "file_ids": [
                        "assistant-o4iT2ksNbunh6hvDEDhwEG5O",
                        "assistant-TOJea0wNrQ2iejJELvGrdCQh"
                      ]
                    }
                  },
                  "metadata": {},
                  "response_format": "auto"
                }
                """;
            var deserialized = json.FromJson<AssistantRequest>();
            Assert.Equal("asst_FFvugfFeVLQzbnaTsF2Mvp31", deserialized.Id);
            Assert.Equal(1.0, deserialized.TopP);
            Assert.Equal(0.5, deserialized.Temperature);
            Assert.Equal("auto", deserialized.ResponseFormat);
            Assert.Equal("auto", deserialized.ResponseFormat!.Dynamic!.ToString());
        }
        [Fact]
        public void RightChoiceWithAttribute()
        {
            var testClass = new ChosenClass
            {
                FirstProperty = new TheSecondChoice()
                {
                    Type = "first",
                    Flexy = 1,
                },
                SecondProperty = new TheSecondChoice1()
                {
                    Type = "first",
                    Flexy = 1,
                }
            };
            var json = testClass.ToJson();
            var deserialized = json.FromJson<ChosenClass>();
            Assert.True(deserialized.FirstProperty!.Is<TheSecondChoice>());
            Assert.True(deserialized.SecondProperty!.Is<TheFirstChoice1>());
            testClass = new ChosenClass
            {
                FirstProperty = new TheSecondChoice()
                {
                    Type = "first",
                    Flexy = 2,
                },
                SecondProperty = new TheSecondChoice1()
                {
                    Type = "third",
                    Flexy = 1,
                }
            };
            json = testClass.ToJson();
            deserialized = json.FromJson<ChosenClass>();
            Assert.True(deserialized.FirstProperty!.Is<TheFirstChoice>());
            Assert.True(deserialized.SecondProperty!.Is<TheSecondChoice1>());
        }
        [Fact]
        public void ChoiceForAttributeAsClass()
        {
            var testClass = new SelectorTestClass { Tests = [] };
            testClass.Tests.Add(new FirstGetClass
            {
                FirstProperty = "first.F",
                SecondProperty = "first.Aloa"
            });
            testClass.Tests.Add(new FirstGetClass
            {
                FirstProperty = "first.F",
                SecondProperty = "first.Aloa"
            });
            testClass.Tests.Add(new SecondGetClass
            {
                FirstProperty = "second.F",
                SecondProperty = "first.Aloa"
            });
            testClass.Tests.Add(new ThirdGetClass
            {
                FirstProperty = "third.F",
                SecondProperty = "first.Aloa"
            });
            testClass.Tests.Add(new FourthGetClass
            {
                FirstProperty = "fourth.F",
                SecondProperty = "first.Aloa"
            });
            testClass.Tests.Add(new FifthGetClass
            {
                FirstProperty = "fifth.A",
                SecondProperty = "first.Aloa"
            });
            testClass.Tests.Add(new FourthGetClass
            {
                FirstProperty = "fourth.F",
                SecondProperty = "first.Aloa"
            });
            testClass.Tests.Add(new FifthGetClass
            {
                FirstProperty = "fifth.Cdas",
                SecondProperty = "first.Aloa"
            });
            testClass.Tests.Add(new FirstGetClass
            {
                FirstProperty = "sixth.AnotherOne",
                SecondProperty = "theDefaultValue"
            });
            var json = testClass.ToJson();
            var deserialized = json.FromJson<SelectorTestClass>();
            Assert.Equal(9, deserialized.Tests!.Count);
            Assert.Equal(2, deserialized.Tests.Count(t => t.IsT0));
            Assert.Equal(1, deserialized.Tests.Count(t => t.IsT1));
            Assert.Equal(1, deserialized.Tests.Count(t => t.IsT2));
            Assert.Equal(2, deserialized.Tests.Count(t => t.IsT3));
            Assert.Equal(2, deserialized.Tests.Count(t => t.IsT4));
            Assert.Equal(1, deserialized.Tests.Count(t => t.IsT5));
        }
        [Fact]
        public void ComplexDeserialization()
        {
            var json = """
                {
                    "id": "run_UHtdBrFiqdaSDbu617vqrie8",
                    "object": "thread.run",
                    "created_at": 1736189952,
                    "assistant_id": "asst_Dx9J9xQ6ii02qlqYIxE5hRon",
                    "thread_id": "thread_NJ3LQPadT2RPvfD6s2v9KW5P",
                    "status": "queued",
                    "started_at": null,
                    "expires_at": 1736190552,
                    "cancelled_at": null,
                    "failed_at": null,
                    "completed_at": null,
                    "required_action": null,
                    "last_error": null,
                    "model": "gpt-4o-2",
                    "instructions": "You are a book reader. When asked a question, use the context to respond to the question.",
                    "tools": [
                        {
                            "type": "code_interpreter"
                        },
                        {
                            "type": "file_search",
                            "file_search": {
                                "max_num_results": 20,
                                "ranking_options": {
                                    "ranker": "default_2024_08_21",
                                    "score_threshold": 0
                                }
                            }
                        }
                    ],
                    "tool_resources": {
                        "code_interpreter": {
                            "file_ids": []
                        }
                    },
                    "metadata": {},
                    "temperature": 0.5,
                    "top_p": 1,
                    "max_completion_tokens": null,
                    "max_prompt_tokens": null,
                    "truncation_strategy": {
                        "type": "auto",
                        "last_messages": null
                    },
                    "incomplete_details": null,
                    "usage": null,
                    "response_format": "auto",
                    "tool_choice": "auto",
                    "parallel_tool_calls": true
                }
                """;
            var deserialized = json.FromJson<AnyOf<RunResult, ThreadMessageResponse, ThreadChunkMessageResponse>>();
            Assert.True(deserialized.IsT0);
        }
        [Fact]
        public void DeserializeAnIncorrectObjectAsNull()
        {
            var json = """
                {
                    "id": "run_UHtdBrFiqdaSDbu617vqrie8",
                    "object": "thread.run"
                }
                """;
            var deserialized = json.FromJson<AnyOf<FirstClass1, SecondClass1>>();
            Assert.Null(deserialized);
            json = """
                {
                    "Test": {
                        "id": "run_UHtdBrFiqdaSDbu617vqrie8",
                        "object": "thread.run"
                    }
                }
                """;
            var deserializedWrapper = json.FromJson<WrapperOfClass1>();
            Assert.Null(deserializedWrapper.Test);
        }
        [Fact]
        public async ValueTask SwitchAndMatchAsync()
        {
            var testClass = new SwitchAndMatchClass
            {
                Test = new FirstClass
                {
                    FirstProperty = "FirstProperty",
                    SecondProperty = "SecondProperty"
                }
            };
            testClass.Test.Switch(
                x => Assert.Equal("FirstProperty", x?.FirstProperty),
                y => Assert.True(false),
                y => Assert.True(false),
                y => Assert.True(false),
                y => Assert.True(false),
                y => Assert.True(false),
                y => Assert.True(false),
                y => Assert.True(false));
            await testClass.Test.SwitchAsync(
                 x => { Assert.Equal("FirstProperty", x?.FirstProperty); return ValueTask.CompletedTask; },
                 y => { Assert.True(false); return ValueTask.CompletedTask; },
                 y => { Assert.True(false); return ValueTask.CompletedTask; },
                 y => { Assert.True(false); return ValueTask.CompletedTask; },
                 y => { Assert.True(false); return ValueTask.CompletedTask; },
                 y => { Assert.True(false); return ValueTask.CompletedTask; },
                 y => { Assert.True(false); return ValueTask.CompletedTask; },
                 y => { Assert.True(false); return ValueTask.CompletedTask; });
            var result = testClass.Test.Match(
                x => { return x?.FirstProperty; },
                y => { return string.Empty; },
                y => { return string.Empty; },
                y => { return string.Empty; },
                y => { return string.Empty; },
                y => { return string.Empty; },
                y => { return string.Empty; },
                y => { return string.Empty; });
            Assert.Equal("FirstProperty", result);
            var resultAsync = await testClass.Test.MatchAsync(
                async x => { await Task.Delay(0); return x?.FirstProperty; },
                async y => { await Task.Delay(0); return string.Empty; },
                async y => { await Task.Delay(0); return string.Empty; },
                async y => { await Task.Delay(0); return string.Empty; },
                async y => { await Task.Delay(0); return string.Empty; },
                async y => { await Task.Delay(0); return string.Empty; },
                async y => { await Task.Delay(0); return string.Empty; });
            Assert.Equal("FirstProperty", resultAsync);
        }
        [Fact]
        public void TryGet()
        {
            var testClass = new SwitchAndMatchClass
            {
                Test = new FirstClass
                {
                    FirstProperty = "FirstProperty",
                    SecondProperty = "SecondProperty"
                }
            };
            Assert.True(testClass.Test.TryGetT0(out var first));
            Assert.False(testClass.Test.TryGetT1(out var second));
            Assert.False(testClass.Test.TryGetT2(out var third));
            Assert.False(testClass.Test.TryGetT3(out var fourth));
            Assert.False(testClass.Test.TryGetT4(out var fifth));
            Assert.False(testClass.Test.TryGetT5(out var sixth));
            Assert.False(testClass.Test.TryGetT6(out var seventh));
            Assert.False(testClass.Test.TryGetT7(out var eighth));
            Assert.Equal("FirstProperty", first!.FirstProperty);
            Assert.Equal("SecondProperty", first!.SecondProperty);
        }
        private sealed class SwitchAndMatchClass
        {
            public AnyOf<FirstClass, decimal, bool, int, SecondClass, RunStatus, string, FirstClass1>? Test { get; set; }
        }
        public sealed class WrapperOfClass1
        {
            public AnyOf<FirstClass1, SecondClass1>? Test { get; set; }
        }
        public sealed class FirstClass1
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        public sealed class SecondClass1
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        private sealed class ChosenClass
        {
            public AnyOf<TheFirstChoice, TheSecondChoice>? FirstProperty { get; set; }
            public AnyOf<TheFirstChoice1, TheSecondChoice1>? SecondProperty { get; set; }
        }
        public sealed class TheFirstChoice
        {
            [AnyOfJsonSelector("first")]
            public string Type { get; init; }
            [AnyOfJsonSelector(2, 3, 4)]
            public int Flexy { get; set; }
        }
        public sealed class TheSecondChoice
        {
            [AnyOfJsonSelector("first", "second")]
            public string Type { get; init; }
            [AnyOfJsonSelector(1)]
            public int Flexy { get; set; }
        }
        public sealed class TheFirstChoice1
        {
            [AnyOfJsonSelector("first")]
            public string Type { get; init; }
            public int Flexy { get; set; }
        }
        public sealed class TheSecondChoice1
        {
            [AnyOfJsonSelector("third", "second")]
            public string Type { get; init; }
            public int Flexy { get; set; }
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
            public List<AssistantVectorStoresFileSearchToolResources>? VectorStores { get; set; }
            [JsonPropertyName("metadata")]
            public Dictionary<string, string>? Metadata { get; set; }
        }
        public sealed class AssistantCodeInterpreterTool
        {
            private const string FileType = "code_interpreter";
            [JsonPropertyName("type")]
            [AnyOfJsonSelector(FileType)]
            public string Type { get; set; } = FileType;
        }
        public sealed class AssistantFileSearchTool
        {
            private const string FileType = "file_search";
            [JsonPropertyName("type")]
            [AnyOfJsonSelector(FileType)]
            public string Type { get; set; } = FileType;
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
            public float ScoreThreshold { get; set; }
        }
        public sealed class AssistantFunctionTool
        {
            private const string FunctionType = "function";
            [JsonPropertyName("type")]
            [AnyOfJsonSelector(FunctionType)]
            public string Type { get; set; } = FunctionType;
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
        public sealed class SelectorTestClass
        {
            public List<AnyOf<FirstGetClass, SecondGetClass, ThirdGetClass, FourthGetClass, FifthGetClass, SixthGetClass>>? Tests { get; set; }
        }
        [AnyOfJsonClassSelector(nameof(FirstProperty), "first.F")]
        public sealed class FirstGetClass
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        [AnyOfJsonRegexClassSelector(nameof(FirstProperty), "secon[^.]*.[^.]*")]
        public sealed class SecondGetClass
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        [AnyOfJsonClassSelector(nameof(FirstProperty), "third.F")]
        public sealed class ThirdGetClass : ThirdGetInnerClass
        {

        }
        public class ThirdGetInnerClass
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        public sealed class FourthGetClass
        {
            [AnyOfJsonSelector("fourth.F")]
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        public sealed class FifthGetClass
        {
            [AnyOfJsonRegexSelector("fift[^.]*.")]
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        [AnyOfJsonDefault]
        public sealed class SixthGetClass
        {
            public string? FirstProperty { get; set; }
            public string? SecondProperty { get; set; }
        }
        public abstract class UnixTimeBaseResponse
        {
            /// The time when the result was generated
            [JsonIgnore]
            public DateTime? Created
            {
                get => CreatedAt.HasValue ? DateTimeOffset.FromUnixTimeSeconds(CreatedAt.Value).DateTime : null;
                set => CreatedAt = value.HasValue ? new DateTimeOffset(value.Value).ToUnixTimeSeconds() : null;
            }
            /// <summary>
            /// The time when the result was generated in unix epoch format
            /// </summary>
            [JsonPropertyName("created")]
            public long? CreatedAt { get; set; }
        }
        public abstract class ApiBaseResponse : UnixTimeBaseResponse
        {
            /// <summary>
            /// Which model was used to generate this result.
            /// </summary>
            [JsonPropertyName("model")]
            public string? Model { get; set; }
            /// <summary>
            /// Object type, ie: text_completion, file, fine-tune, list, etc
            /// </summary>
            [JsonPropertyName("object")]
            public string? Object { get; set; }
            /// <summary>
            /// The organization associated with the API request, as reported by the API.
            /// </summary>
            [JsonIgnore]
            public string? Organization { get; internal set; }
            /// <summary>
            /// The server-side processing time as reported by the API.  This can be useful for debugging where a delay occurs.
            /// </summary>
            [JsonIgnore]
            public TimeSpan ProcessingTime { get; internal set; }
            /// <summary>
            /// The request id of this API call, as reported in the response headers.  This may be useful for troubleshooting or when contacting OpenAI support in reference to a specific request.
            /// </summary>
            [JsonIgnore]
            public string? RequestId { get; internal set; }
            /// <summary>
            /// The Openai-Version used to generate this response, as reported in the response headers.  This may be useful for troubleshooting or when contacting OpenAI support in reference to a specific request.
            /// </summary>
            [JsonIgnore]
            public string? OpenaiVersion { get; internal set; }
        }
        public enum RunStatus
        {
            Queued,
            InProgress,
            RequiresAction,
            Cancelling,
            Cancelled,
            Failed,
            Completed,
            Incomplete,
            Expired
        }
        public enum LastErrorCode
        {
            ServerError,
            RateLimitExceeded,
            InvalidPrompt
        }
        public sealed class LastErrorRun
        {
            [JsonPropertyName("code")]
            public string? CodeAsString { get; set; }
            [JsonIgnore]
            public LastErrorCode? Code => LastErrorCodeExtensions.ToLastErrorCode(CodeAsString);
            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
        public sealed class IncompleteReasonRun
        {
            [JsonPropertyName("reason")]
            public string? Reason { get; set; }
        }
        public class ThreadMessage
        {
            [JsonPropertyName("role")]
            public string? Role { get; set; }
            [JsonPropertyName("content")]
            public AnyOf<string, List<ChatMessageContent>>? Content { get; set; }
            [JsonPropertyName("attachments")]
            public List<ThreadAttachment>? Attachments { get; set; }
            [JsonPropertyName("metadata")]
            public Dictionary<string, string>? Metadata { get; set; }
            [JsonIgnore]
            public bool AlreadyAdded { get; set; } = true;
        }
        public sealed class ThreadAttachmentTool
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            public static ThreadAttachmentTool CodeInterpreter { get; } = new ThreadAttachmentTool
            {
                Type = "code_interpreter"
            };
            public static ThreadAttachmentTool FileSearch { get; } = new ThreadAttachmentTool
            {
                Type = "file_search"
            };
        }
        public sealed class ThreadChunkMessageResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("object")]
            [AnyOfJsonSelector("thread.message.delta")]
            public string? Object { get; set; }
            [JsonPropertyName("delta")]
            public ThreadDeltaMessageResponse? Delta { get; set; }
        }
        public sealed class ThreadDeltaMessageResponse
        {
            [JsonPropertyName("content")]
            public AnyOf<string, List<ChatMessageContent>>? Content { get; set; }
        }
        public sealed class ThreadAttachment
        {
            [JsonPropertyName("file_id")]
            public string? FileId { get; set; }
            [JsonPropertyName("tools")]
            public List<ThreadAttachmentTool>? Tools { get; set; }
        }
        public sealed class ChatMessageTextContent
        {
            [JsonPropertyName("value")]
            public string? Value { get; set; }
            [JsonPropertyName("annotations")]
            public List<string>? Annotations { get; set; }
        }
        public sealed class ChatMessageImageContent
        {
            [JsonPropertyName("url")]
            public string? Url { get; set; }
            [JsonPropertyName("detail")]
            public string? Detail { get; set; }
        }
        public sealed class ChatMessageContent
        {
            [JsonPropertyName("index")]
            public int? Index { get; set; }
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("text")]
            public AnyOf<string, ChatMessageTextContent>? Text { get; set; }
            [JsonPropertyName("image_url")]
            public ChatMessageImageContent? Image { get; set; }
            [JsonPropertyName("image_file")]
            public ChatMessageImageFile? FileImage { get; set; }
            [JsonPropertyName("input_audio")]
            public ChatMessageAudioFile? AudioInput { get; set; }
            [JsonPropertyName("refusal")]
            public string? Refusal { get; set; }
        }
        public sealed class ChatMessageAudioFile
        {
            [JsonPropertyName("data")]
            public string? Data { get; set; }
            [JsonPropertyName("format")]
            public string? Format { get; set; }
        }
        public sealed class ChatMessageImageFile
        {
            [JsonPropertyName("file_id")]
            public string? FileId { get; set; }
            [JsonPropertyName("detail")]
            public string? Detail { get; set; }
        }
        public sealed class ThreadMessageResponse : ThreadMessage
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("object")]
            [AnyOfJsonSelector("thread.message")]
            public string? Object { get; set; }
            [JsonPropertyName("assistant_id")]
            public string? AssistantId { get; set; }
            [JsonPropertyName("thread_id")]
            public string? ThreadId { get; set; }
            /// The time when the result was generated
            [JsonIgnore]
            public DateTime? Created
            {
                get => CreatedAt.HasValue ? DateTimeOffset.FromUnixTimeSeconds(CreatedAt.Value).DateTime : null;
                set => CreatedAt = value.HasValue ? new DateTimeOffset(value.Value).ToUnixTimeSeconds() : null;
            }
            /// <summary>
            /// The time when the result was generated in unix epoch format
            /// </summary>
            [JsonPropertyName("created_at")]
            public long? CreatedAt { get; set; }
            [JsonPropertyName("run_id")]
            public string? RunId { get; set; }
        }
        [AnyOfJsonDefault]
        public sealed class RunResult : ApiBaseResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("assistant_id")]
            public string? AssistantId { get; set; }
            [JsonPropertyName("thread_id")]
            public string? ThreadId { get; set; }
            [JsonPropertyName("status")]
            public string? StatusAsString { get; set; }
            [JsonIgnore]
            public RunStatus? Status => RunStatusExtensions.ToRunStatus(StatusAsString);
            [JsonPropertyName("started_at")]
            public int? StartedAt { get; set; }
            [JsonPropertyName("expires_at")]
            public int? ExpiresAt { get; set; }
            [JsonPropertyName("cancelled_at")]
            public int? CancelledAt { get; set; }
            [JsonPropertyName("failed_at")]
            public int? FailedAt { get; set; }
            [JsonPropertyName("completed_at")]
            public int? CompletedAt { get; set; }
            [JsonPropertyName("last_error")]
            public LastErrorRun? LastError { get; set; }
            [JsonPropertyName("instructions")]
            public string? Instructions { get; set; }
            [JsonPropertyName("incomplete_details")]
            public IncompleteReasonRun? IncompleteDetails { get; set; }
            [JsonPropertyName("tools")]
            public List<AnyOf<AssistantFunctionTool, AssistantCodeInterpreterTool, AssistantFileSearchTool>>? Tools { get; set; }
            [JsonPropertyName("metadata")]
            public Dictionary<string, string>? Metadata { get; set; }
            [JsonPropertyName("usage")]
            public ChatUsage? Usage { get; set; }
            [JsonPropertyName("temperature")]
            public double? Temperature { get; set; }
            [JsonPropertyName("top_p")]
            public double? TopP { get; set; }
            [JsonPropertyName("max_prompt_tokens")]
            public int? MaxPromptTokens { get; set; }
            [JsonPropertyName("max_completion_tokens")]
            public int? MaxCompletionTokens { get; set; }
            [JsonPropertyName("truncation_strategy")]
            public RunTruncationStrategy? TruncationStrategy { get; set; }
            [JsonPropertyName("response_format")]
            public AnyOf<string, ResponseFormatChatRequest>? ResponseFormat { get; set; }
            [JsonPropertyName("tool_choice")]
            public AnyOf<string, ForcedFunctionTool>? ToolChoice { get; set; }
            [JsonPropertyName("parallel_tool_calls")]
            public bool ParallelToolCalls { get; set; }
        }
        public sealed class RunTruncationStrategy
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("last_messages")]
            public int? NumberOfLastMessages { get; set; }
            private const string TruncationTypeLastMessages = "last_messages";
            public static RunTruncationStrategy Auto { get; } = new() { Type = "auto" };
            public static RunTruncationStrategy LastMessages(int numberOfMessages) => new() { Type = TruncationTypeLastMessages, NumberOfLastMessages = numberOfMessages };
        }
        public sealed class ForcedFunctionTool
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("function")]
            public ForcedFunctionToolName? Function { get; set; }
        }
        public sealed class ForcedFunctionToolName
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }
        public sealed class ChatUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }

            [JsonPropertyName("completion_tokens_details")]
            public CompletionTokensDetails? CompletionTokensDetails { get; set; }
        }
        public sealed class CompletionTokensDetails
        {
            [JsonPropertyName("reasoning_tokens")]
            public int? ReasoningTokens { get; set; }

            [JsonPropertyName("accepted_prediction_tokens")]
            public int? AcceptedPredictionTokens { get; set; }

            [JsonPropertyName("rejected_prediction_tokens")]
            public int? RejectedPredictionTokens { get; set; }
        }
        internal static class RunStatusExtensions
        {
            public static RunStatus? ToRunStatus(string? input)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    return null;
                }

                return input switch
                {
                    "queued" => RunStatus.Queued,
                    "in_progress" => RunStatus.InProgress,
                    "requires_action" => RunStatus.RequiresAction,
                    "cancelling" => RunStatus.Cancelling,
                    "cancelled" => RunStatus.Cancelled,
                    "failed" => RunStatus.Failed,
                    "completed" => RunStatus.Completed,
                    "incomplete" => RunStatus.Incomplete,
                    "expired" => RunStatus.Expired,
                    _ => null
                };
            }
        }
        internal static class LastErrorCodeExtensions
        {
            public static LastErrorCode? ToLastErrorCode(string? input)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    return null;
                }

                return input switch
                {
                    "server_error" => LastErrorCode.ServerError,
                    "rate_limit_exceeded" => LastErrorCode.RateLimitExceeded,
                    "invalid_prompt" => LastErrorCode.InvalidPrompt,
                    _ => null
                };
            }
        }
    }
}
