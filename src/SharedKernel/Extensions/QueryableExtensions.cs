using System.Linq.Expressions;

namespace SharedKernel.Extensions;

public static class QueryableExtensions
{
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