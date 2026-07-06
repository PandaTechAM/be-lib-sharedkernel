using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedKernel.JsonConverters;

/// <summary>
///     Creates enum JSON converters that serialize enums as strings, supporting both nullable and non-nullable enums.
/// </summary>
public class EnumConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum ||
               (Nullable.GetUnderlyingType(typeToConvert)?.IsEnum ?? false);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
        if (underlyingType != null) // It's a nullable enum
        {
            var converterType = typeof(NullableEnumConverter<>).MakeGenericType(underlyingType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
        else // Non-nullable enum
        {
            var converterType = typeof(NonNullableEnumConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}
