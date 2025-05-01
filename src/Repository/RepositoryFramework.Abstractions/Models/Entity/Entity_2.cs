using System.Text.Json;
using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    [JsonConverter(typeof(EntityJsonConverterFactory))]
    public class Entity<T, TKey>
        where TKey : notnull
    {
        public TKey? Key { get; set; }
        public T? Value { get; set; }
        [JsonIgnore]
        public bool HasValue => Value != null;
        [JsonIgnore]
        public bool HasKey => Key != null;
        public static Entity<T, TKey> Default(T value, TKey key)
            => new(value, key);
        public Entity(T? value = default, TKey? key = default)
        {
            Value = value;
            Key = key;
        }
        public State<T, TKey> ToOkState()
            => State.Ok(this);
        public State<T, TKey> ToNotOkState()
            => State.NotOk(this);
    }
    public class EntityJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType &&
                   typeToConvert.GetGenericTypeDefinition() == typeof(Entity<,>);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            Type[] typeArgs = type.GetGenericArguments();
            Type converterType = typeof(EntityJsonConverter<,>).MakeGenericType(typeArgs);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    public class EntityJsonConverter<T, TKey> : JsonConverter<Entity<T, TKey>>
        where TKey : notnull
    {
        public override Entity<T, TKey>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            TKey? key = default;
            T? value = default;

            if (root.TryGetProperty("k", out var kProp))
                key = JsonSerializer.Deserialize<TKey>(kProp.GetRawText(), options);
            else if (root.TryGetProperty("Key", out var keyProp))
                key = JsonSerializer.Deserialize<TKey>(keyProp.GetRawText(), options);

            if (root.TryGetProperty("v", out var vProp))
                value = JsonSerializer.Deserialize<T>(vProp.GetRawText(), options);
            else if (root.TryGetProperty("Value", out var valueProp))
                value = JsonSerializer.Deserialize<T>(valueProp.GetRawText(), options);

            return new Entity<T, TKey>(value, key);
        }

        public override void Write(Utf8JsonWriter writer, Entity<T, TKey> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("k");
            JsonSerializer.Serialize(writer, value.Key, options);

            writer.WritePropertyName("v");
            JsonSerializer.Serialize(writer, value.Value, options);

            writer.WriteEndObject();
        }
    }

}
