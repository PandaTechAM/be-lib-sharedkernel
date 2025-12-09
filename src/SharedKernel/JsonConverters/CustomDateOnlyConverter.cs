using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedKernel.JsonConverters;

public class CustomDateOnlyConverter : JsonConverter<DateOnly>
{
   private const string DateFormat = "dd-MM-yyyy";
   public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      var dateString = reader.GetString();

      if (DateOnly.TryParseExact(dateString, DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
      {
         return date;
      }

      throw new JsonException($"Unable to parse date: {dateString}");
   }

   public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
   {
      writer.WriteStringValue(value.ToString(DateFormat));
   }
}