using System;
using System.Collections.Generic;
using System.Linq;
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
            Assert.True(deserialized.Test!.Is<SignatureClassOne>());
            Assert.False(deserialized.Test!.Is<SignatureClassTwo>());
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
    }
}
