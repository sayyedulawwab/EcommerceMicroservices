using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedKernel.Domain;

namespace Cart.Infrastructure.Caching;

/// <summary>
/// Currency has a private constructor and a get-only Code, so Newtonsoft can't rebuild it. It is written as
/// <c>{"Code":"USD"}</c> (the default shape) and read back through <see cref="Currency.FromStoredCode"/>.
/// </summary>
public sealed class CurrencyJsonConverter : JsonConverter<Currency>
{
    public override void WriteJson(JsonWriter writer, Currency? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(Currency.Code));
        writer.WriteValue(value?.Code);
        writer.WriteEndObject();
    }

    public override Currency? ReadJson(JsonReader reader, Type objectType, Currency? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var json = JObject.Load(reader);

        return Currency.FromStoredCode(json.Value<string>(nameof(Currency.Code)) ?? string.Empty);
    }
}
