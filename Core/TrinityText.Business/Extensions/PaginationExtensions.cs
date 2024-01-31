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
            if (sorting.HasValue)
            {
                if (list.Expression.Type == typeof(IOrderedQueryable<X>))
                {

                    if (sorting.Value == SortingType.Ascending)
                    {
                        list = (list as IOrderedQueryable<X>).ThenBy(field);
                    }
                    if (sorting.Value == SortingType.Descending)
                    {
                        list = (list as IOrderedQueryable<X>).ThenByDescending(field);
                    }
                }
                else
                {
                    if (sorting.Value == SortingType.Ascending)
                    {
                        list = (list as IOrderedQueryable<X>).OrderBy(field);
                    }
                    if (sorting.Value == SortingType.Descending)
                    {
                        list = (list as IOrderedQueryable<X>).OrderByDescending(field);
                    }
                }
            }
            return list;
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
