namespace SharedKernel.Extensions;

public static class EnumerableExtensions
{
   public static bool In<T>(this T value, params T[] values)
   {
      return values.Contains(value);
   }

   public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> query, bool condition, Func<T, bool> expression)
   {
      return condition ? query.Where(expression) : query;
   }
}