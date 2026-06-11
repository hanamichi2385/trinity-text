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

            if (list is IOrderedQueryable<X> ordered)
            {
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
