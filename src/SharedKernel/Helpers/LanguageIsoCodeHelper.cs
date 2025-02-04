namespace SharedKernel.Helpers;

public static class LanguageIsoCodeHelper
{
   private static Dictionary<string, string> AvailableLanguages =>
      new()
      {
         {
            "en-US", "English (United States)"
         },
         {
            "en-CA", "English (Canada)"
         },
         {
            "ar-BH", "Arabic (Bahrain)"
         },
         {
            "ar-KW", "Arabic (Kuwait)"
         },
         {
            "ar-LB", "Arabic (Lebanon)"
         },
         {
            "ar-OM", "Arabic (Oman)"
         },
         {
            "ar-QA", "Arabic (Qatar)"
         },
         {
            "ar-SA", "Arabic (Saudi Arabia)"
         },
         {
            "ar-AE", "Arabic (UAE)"
         },
         {
            "bs-BA", "Bosnian (Bosnia and Herzegovina)"
         },
         {
            "bg-BG", "Bulgarian (Bulgaria)"
         },
         {
            "ca-ES", "Catalan (Spain)"
         },
         {
            "hr-HR", "Croatian (Croatia)"
         },
         {
            "cs-CZ", "Czech (Czech Republic)"
         },
         {
            "da-DK", "Danish (Denmark)"
         },
         {
            "de-AT", "German (Austria)"
         },
         {
            "de-CH", "German (Switzerland)"
         },
         {
            "de-DE", "German (Germany)"
         },
         {
            "de-LU", "German (Luxembourg)"
         },
         {
            "el-GR", "Greek (Greece)"
         },
         {
            "en-AE", "English (UAE)"
         },
         {
            "en-AR", "English (Argentina)"
         },
         {
            "en-AU", "English (Australia)"
         },
         {
            "en-AT", "English (Austria)"
         },
         {
            "en-BG", "English (Bulgaria)"
         },
         {
            "en-BH", "English (Bahrain)"
         },
         {
            "en-BR", "English (Brazil)"
         },
         {
            "en-CL", "English (Chile)"
         },
         {
            "en-CO", "English (Colombia)"
         },
         {
            "en-HR", "English (Croatia)"
         },
         {
            "en-CY", "English (Cyprus)"
         },
         {
            "en-CZ", "English (Czech Republic)"
         },
         {
            "en-DK", "English (Denmark)"
         },
         {
            "en-FI", "English (Finland)"
         },
         {
            "en-DE", "English (Germany)"
         },
         {
            "en-GB", "English (Great Britain)"
         },
         {
            "en-GR", "English (Greece)"
         },
         {
            "en-HK", "English (Hong Kong)"
         },
         {
            "en-HU", "English (Hungary)"
         },
         {
            "en-IS", "English (Iceland)"
         },
         {
            "en-IN", "English (India)"
         },
         {
            "en-ID", "English (Indonesia)"
         },
         {
            "en-IE", "English (Ireland)"
         },
         {
            "en-IL", "English (Israel)"
         },
         {
            "en-KW", "English (Kuwait)"
         },
         {
            "en-LV", "English (Latvia)"
         },
         {
            "en-LB", "English (Lebanon)"
         },
         {
            "en-MY", "English (Malaysia)"
         },
         {
            "en-MT", "English (Malta)"
         },
         {
            "en-MX", "English (Mexico)"
         },
         {
            "en-NL", "English (Netherlands)"
         },
         {
            "en-NZ", "English (New Zealand)"
         },
         {
            "en-NO", "English (Norway)"
         },
         {
            "en-OM", "English (Oman)"
         },
         {
            "en-PE", "English (Peru)"
         },
         {
            "en-PL", "English (Poland)"
         },
         {
            "en-QA", "English (Qatar)"
         },
         {
            "en-RO", "English (Romania)"
         },
         {
            "en-SA", "English (Saudi Arabia)"
         },
         {
            "en-SE", "English (Sweden)"
         },
         {
            "en-SG", "English (Singapore)"
         },
         {
            "en-SK", "English (Slovakia)"
         },
         {
            "en-SI", "English (Slovenia)"
         },
         {
            "en-TW", "English (Taiwan)"
         },
         {
            "en-TH", "English (Thailand)"
         },
         {
            "en-TR", "English (Turkey)"
         },
         {
            "en-ZA", "English (South Africa)"
         },
         {
            "et-EE", "Estonian (Estonia)"
         },
         {
            "fi-FI", "Finnish (Finland)"
         },
         {
            "fr-BE", "French (Belgium)"
         },
         {
            "fr-CA", "French (Canada)"
         },
         {
            "fr-FR", "French (France)"
         },
         {
            "fr-LU", "French (Luxembourg)"
         },
         {
            "fr-CH", "French (Switzerland)"
         },
         {
            "he-IL", "Hebrew (Israel)"
         },
         {
            "hi-IN", "Hindi (India)"
         },
         {
            "hu-HU", "Hungarian (Hungary)"
         },
         {
            "is-IS", "Icelandic (Iceland)"
         },
         {
            "id-ID", "Indonesian (Indonesia)"
         },
         {
            "it-CH", "Italian (Switzerland)"
         },
         {
            "it-IT", "Italian (Italy)"
         },
         {
            "ja-JP", "Japanese (Japan)"
         },
         {
            "kk-KZ", "Kazakh (Kazakhstan)"
         },
         {
            "ko-KR", "Korean (Korea)"
         },
         {
            "lt-LT", "Lithuanian (Lithuania)"
         },
         {
            "lv-LV", "Latvian (Latvia)"
         },
         {
            "mg-MG", "Malagasy (Madagascar)"
         },
         {
            "mk-MK", "Macedonian (Macedonia)"
         },
         {
            "ms-MY", "Malay (Malaysia)"
         },
         {
            "nb-NO", "Norwegian Bokmal (Norway)"
         },
         {
            "nl-BE", "Dutch (Belgium)"
         },
         {
            "nl-NL", "Dutch (Netherlands)"
         },
         {
            "nn-NO", "Norwegian Nynorsk (Norway)"
         },
         {
            "no-NO", "Norwegian (Norway)"
         },
         {
            "fa-IR", "Persian (Iran)"
         },
         {
            "pl-PL", "Polish (Poland)"
         },
         {
            "pt-BR", "Portuguese (Brazil)"
         },
         {
            "pt-PT", "Portuguese (Portugal)"
         },
         {
            "ro-RO", "Romanian (Romania)"
         },
         {
            "ru-RU", "Russian (Russia)"
         },
         {
            "ru-UA", "Russian (Ukraine)"
         },
         {
            "zh-CN", "Simplified Chinese (China)"
         },
         {
            "zh-HK", "Simplified Chinese (Hong Kong)"
         },
         {
            "zh-TW", "Simplified Chinese (Taiwan)"
         },
         {
            "sr-ME", "Serbian (Montenegro)"
         },
         {
            "sr-RS", "Serbian (Serbia)"
         },
         {
            "sk-SK", "Slovak (Slovakia)"
         },
         {
            "sl-SI", "Slovenian (Slovenia)"
         },
         {
            "es-AR", "Spanish (Argentina)"
         },
         {
            "es-BO", "Spanish (Bolivia)"
         },
         {
            "es-CL", "Spanish (Chile)"
         },
         {
            "es-CO", "Spanish (Colombia)"
         },
         {
            "es-CR", "Spanish (Costa Rica)"
         },
         {
            "es-DO", "Spanish (Dominican Republic)"
         },
         {
            "es-EC", "Spanish (Ecuador)"
         },
         {
            "es-SV", "Spanish (El Salvador)"
         },
         {
            "es-GT", "Spanish (Guatemala)"
         },
         {
            "es-HN", "Spanish (Honduras)"
         },
         {
            "es-LA", "Spanish (Latin America)"
         },
         {
            "es-MX", "Spanish (Mexico)"
         },
         {
            "es-NI", "Spanish (Nicaragua)"
         },
         {
            "es-PA", "Spanish (Panama)"
         },
         {
            "es-PE", "Spanish (Peru)"
         },
         {
            "es-PY", "Spanish (Paraguay)"
         },
         {
            "es-ES", "Spanish (Spain)"
         },
         {
            "es-US", "Spanish (United States)"
         },
         {
            "es-UY", "Spanish (Uruguay)"
         },
         {
            "sv-SE", "Swedish (Sweden)"
         },
         {
            "tl-PH", "Tagalog (Philippines)"
         },
         {
            "th-TH", "Thai (Thailand)"
         },
         {
            "ch-HK", "Traditional Chinese (Hong Kong)"
         },
         {
            "ch-TW", "Traditional Chinese (Taiwan)"
         },
         {
            "tr-TR", "Turkish (Turkey)"
         },
         {
            "uk-UA", "Ukrainian (Ukraine)"
         },
         {
            "vi-VN", "Vietnamese (Vietnam)"
         },
         {
            "af-ZA", "Afrikaans (South Africa)"
         },
         {
            "ar-EG", "Arabic (Egypt)"
         },
         {
            "as-IN", "Assamese (India)"
         },
         {
            "az-AZ", "Azerbaijani (Azerbaijan)"
         },
         {
            "bn-BD", "Bengali (Bangladesh)"
         },
         {
            "cy-GB", "Welsh (Wales)"
         },
         {
            "eu-ES", "Basque (Spain)"
         },
         {
            "ff-SN", "Fula (Senegal)"
         },
         {
            "gl-ES", "Galician (Spain)"
         },
         {
            "gu-IN", "Gujarati (India)"
         },
         {
            "ha-NG", "Hausa (Nigeria)"
         },
         {
            "ht-HT", "Haitian Creole (Haiti)"
         },
         {
            "hy-AM", "Armenian (Armenia)"
         },
         {
            "ig-NG", "Igbo (Nigeria)"
         },
         {
            "jv-ID", "Javanese (Indonesia)"
         },
         {
            "ka-GE", "Georgian (Georgia)"
         },
         {
            "km-KH", "Khmer (Cambodia)"
         },
         {
            "kn-IN", "Kannada (India)"
         },
         {
            "ku-TR", "Kurdish (Turkey)"
         },
         {
            "ky-KG", "Kyrgyz (Kyrgyzstan)"
         },
         {
            "mi-NZ", "Maori (New Zealand)"
         },
         {
            "ml-IN", "Malayalam (India)"
         },
         {
            "mn-MN", "Mongolian (Mongolia)"
         },
         {
            "mr-IN", "Marathi (India)"
         },
         {
            "my-MM", "Burmese (Myanmar)"
         },
         {
            "ne-NP", "Nepali (Nepal)"
         },
         {
            "om-ET", "Oromo (Ethiopia)"
         },
         {
            "pa-IN", "Punjabi (India)"
         },
         {
            "pa-PK", "Punjabi (Pakistan)"
         },
         {
            "sd-IN", "Sindhi (India)"
         },
         {
            "si-LK", "Sinhala (Sri Lanka)"
         },
         {
            "sn-ZW", "Shona (Zimbabwe)"
         },
         {
            "so-SO", "Somali (Somalia)"
         },
         {
            "sq-AL", "Albanian (Albania)"
         },
         {
            "su-ID", "Sundanese (Indonesia)"
         },
         {
            "sw-KE", "Swahili (Kenya)"
         },
         {
            "ta-IN", "Tamil (India)"
         },
         {
            "te-IN", "Telugu (India)"
         },
         {
            "ur-PK", "Urdu (Pakistan)"
         },
         {
            "uz-UZ", "Uzbek (Uzbekistan)"
         },
         {
            "wo-SN", "Wolof (Senegal)"
         },
         {
            "xh-ZA", "Xhosa (South Africa)"
         },
         {
            "yo-NG", "Yoruba (Nigeria)"
         },
         {
            "zu-ZA", "Zulu (South Africa)"
         },
         {
            "es-419", "Spanish (Latin America)"
         },
         {
            "zh-Hans-HK", "Simplified Chinese (Hong Kong)"
         },
         {
            "zh-Hant-HK", "Traditional Chinese (Hong Kong)"
         },
         {
            "zh-Hans-TW", "Simplified Chinese (Taiwan)"
         },
         {
            "zh-Hant-TW", "Traditional Chinese (Taiwan)"
         },
         {
            "ceb-PH", "Cebuano (Philippines)"
         },
         {
            "mai-IN", "Maithili (India)"
         }
      };

   public static bool IsValidLanguageCode(string isoCode)
   {
      return AvailableLanguages.ContainsKey(isoCode);
   }

   public static string? GetName(string isoCode)
   {
      if (!AvailableLanguages.ContainsKey(isoCode))
      {
         return null;
      }

      AvailableLanguages.TryGetValue(isoCode, out var name);
      return name;
   }

   public static string? GetCode(string codeName)
   {
      if (!AvailableLanguages.ContainsValue(codeName))
      {
         return null;
      }

      var pair = AvailableLanguages.FirstOrDefault(x => x.Value == codeName);
      return pair.Key;
   }
}