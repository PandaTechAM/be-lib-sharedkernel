using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharedKernel.Extensions;

public static class DictionaryExtensions
{
   public static TValue? GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue? value)
      where TKey : notnull
   {
      ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);

      if (exists)
      {
         return val;
      }

      val = value;
      return val;
   }

   public static bool TryUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
      where TKey : notnull
   {
      ref var val = ref CollectionsMarshal.GetValueRefOrNullRef(dict, key);
      if (Unsafe.IsNullRef(ref val))
      {
         return false;
      }

      val = value;
      return true;
   }
}