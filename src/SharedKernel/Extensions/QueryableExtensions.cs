using System.Linq.Expressions;

namespace SharedKernel.Extensions;

/// <summary>
///     LINQ <see cref="IQueryable{T}" /> extension methods.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    ///     Applies the given predicate to the query only when <paramref name="condition" /> is true.
    /// </summary>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> expression)
    {
        if (condition)
        {
            return query.Where(expression);
        }

        return query;
    }
}
