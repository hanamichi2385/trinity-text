using System;
using System.Collections.Generic;
using System.Linq;

namespace TrinityText.Business
{
    public static class PaginationExtensions
    {
        public static IQueryable<T> GetPage<T>(this IQueryable<T> list, int page, int size = 100)
        {
            return list
                .Skip(page * size)
                .Take(size);
        }
        public static IQueryable<X> Sort<X, Y>(this IQueryable<X> list, System.Linq.Expressions.Expression<Func<X, Y>> field, SortingType? sorting)
        {
            if (!sorting.HasValue || sorting.Value == SortingType.Unordered)
            {
                return list;
            }

            // Check the LINQ expression's declared type, not the runtime type:
            // EF Core's queryable concrete class implements IOrderedQueryable<T> even before an OrderBy is applied,
            // so `list is IOrderedQueryable<X>` would always be true. The expression type instead correctly
            // becomes IOrderedQueryable<X> only after OrderBy/OrderByDescending.
            bool alreadyOrdered = typeof(IOrderedQueryable<X>).IsAssignableFrom(list.Expression.Type);

            if (alreadyOrdered)
            {
                var ordered = (IOrderedQueryable<X>)list;
                return sorting.Value == SortingType.Ascending
                    ? ordered.ThenBy(field)
                    : ordered.ThenByDescending(field);
            }

            return sorting.Value == SortingType.Ascending
                ? list.OrderBy(field)
                : list.OrderByDescending(field);
        }
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Result { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int PageCount
        {
            get
            {
                if (PageSize > 0)
                {
                    return TotalCount / PageSize + (TotalCount % PageSize != 0 ? 1 : 0);
                }
                return 0;
            }
        }

        public int TotalCount { get; set; }
    }

    public abstract class PagedRequest
    {
        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}
