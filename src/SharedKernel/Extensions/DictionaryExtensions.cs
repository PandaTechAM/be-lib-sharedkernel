using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharedKernel.Extensions;

public static class DictionaryExtensions
{
   extension<TKey, TValue>(Dictionary<TKey, TValue> dict) where TKey : notnull
   {
      public TValue? GetOrAdd(TKey key, TValue? value)
      {
         ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);

         if (exists)
         {
            return val;
         }

         val = value;
         return val;
      }

      public bool TryUpdate(TKey key, TValue value)
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
}