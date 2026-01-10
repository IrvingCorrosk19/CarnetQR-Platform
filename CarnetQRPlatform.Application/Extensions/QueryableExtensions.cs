using Microsoft.EntityFrameworkCore;
using CarnetQRPlatform.Application.Common;

namespace CarnetQRPlatform.Application.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, 
        int pageNumber, 
        int pageSize)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, 
        PaginationParameters parameters)
    {
        return await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize);
    }
}
