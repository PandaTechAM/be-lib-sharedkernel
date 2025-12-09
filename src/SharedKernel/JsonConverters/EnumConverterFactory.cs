using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SharedKernel.JsonConverters;

public class EnumConverterFactory : JsonConverterFactory
{
   public override bool CanConvert(Type typeToConvert)
   {
      return typeToConvert.IsEnum ||
             (Nullable.GetUnderlyingType(typeToConvert)?.IsEnum ?? false);
   }

   public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
   {
      var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
      if (underlyingType != null) // It's a nullable enum
      {
         Type converterType = typeof(NullableEnumConverter<>).MakeGenericType(underlyingType);
         return (JsonConverter)Activator.CreateInstance(converterType)!;
      }
      else // Non-nullable enum
      {
         Type converterType = typeof(NonNullableEnumConverter<>).MakeGenericType(typeToConvert);
         return (JsonConverter)Activator.CreateInstance(converterType)!;
      }
   }
}