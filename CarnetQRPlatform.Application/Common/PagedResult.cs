namespace CarnetQRPlatform.Application.Common;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class PaginationParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    public PaginationParameters()
    {
    }

    public PaginationParameters(int pageNumber, int pageSize)
    {
        PageNumber = Math.Max(1, pageNumber);
        PageSize = Math.Min(100, Math.Max(1, pageSize)); // Limitar a máximo 100 por página
    }
}
