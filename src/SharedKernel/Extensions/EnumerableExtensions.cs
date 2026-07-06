namespace SharedKernel.Extensions;

/// <summary>
///     Provides small LINQ-style convenience helpers.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    ///     Return whether <paramref name="value" /> is contained in <paramref name="values" />.
    /// </summary>
    public static bool In<T>(this T value, params T[] values)
    {
        return values.Contains(value);
    }

    /// <summary>
    ///     Apply the <paramref name="expression" /> filter only when <paramref name="condition" /> is true.
    /// </summary>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> query, bool condition, Func<T, bool> expression)
    {
        return condition ? query.Where(expression) : query;
    }
}
