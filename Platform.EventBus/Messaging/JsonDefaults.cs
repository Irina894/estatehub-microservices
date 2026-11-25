using System.Text.Json;
using System.Text.Json.Serialization;

namespace Platform.EventBus.Messaging;

/// <summary>Єдині правила серіалізації для publisher і consumer: camelCase, enum рядками, дати ISO 8601.</summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
