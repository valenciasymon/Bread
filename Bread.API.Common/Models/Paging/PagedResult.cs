namespace Bread.API.Common.Models.Paging;

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public PaginationMetaData MetaData { get; set; } = new();
}
