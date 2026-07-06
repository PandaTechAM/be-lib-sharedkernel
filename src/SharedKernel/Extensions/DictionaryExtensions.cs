using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharedKernel.Extensions;

/// <summary>
///     Provides low-allocation dictionary lookup/update helpers built on <see cref="CollectionsMarshal" />.
/// </summary>
public static class DictionaryExtensions
{
    extension<TKey, TValue>(Dictionary<TKey, TValue> dict) where TKey : notnull
    {
        /// <summary>
        ///     Return the existing value for <paramref name="key" />, or add <paramref name="value" /> and return it.
        /// </summary>
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

        /// <summary>
        ///     Update the value for <paramref name="key" /> if it exists, without adding a new entry.
        /// </summary>
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
