using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedKernel.JsonConverters;

public class NullableEnumConverter<T> : JsonConverter<T?> where T : struct, Enum
{
   public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (reader.TokenType == JsonTokenType.Null)
      {
         return null;
      }

      if (reader.TokenType == JsonTokenType.String)
      {
         var enumString = reader.GetString();
         if (Enum.TryParse(enumString, true, out T result))
         {
            return result;
         }
      }
      else if (reader.TokenType == JsonTokenType.Number)
      {
         int enumValue = reader.GetInt32();
         if (Enum.IsDefined(typeof(T), enumValue))
         {
            return (T)Enum.ToObject(typeof(T), enumValue);
         }
      }

      throw new JsonException($"Unable to convert value to nullable enum {typeof(T)}");
   }

   public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
   {
      if (value.HasValue)
      {
         writer.WriteStringValue(value.Value.ToString());
      }
      else
      {
         writer.WriteNullValue();
      }
   }
}