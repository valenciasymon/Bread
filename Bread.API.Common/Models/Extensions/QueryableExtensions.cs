
using Microsoft.EntityFrameworkCore;
using Bread.API.Common.Models.Paging;

namespace Bread.API.Common.Models.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResult<T>(
        this IQueryable<T> source,
        PaginationParameters paginationParams)
    {
        int totalCount = await source.CountAsync();

        List<T> items = await source
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        int totalPages = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize);

        PaginationMetaData metaData = new PaginationMetaData
        {
            CurrentPage = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNext = paginationParams.PageNumber < totalPages,
            HasPrevious = paginationParams.PageNumber > 1
        };

        return new PagedResult<T>
        {
            Data = items,
            MetaData = metaData
        };
    }
}
