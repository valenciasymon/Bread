using System.ComponentModel.DataAnnotations;

namespace Bread.API.Common.Models.Paging;
public class PaginationParameters
{
    private const int MaxPageSize = 30;
    private int _pagesize = 10;

    [Range(1, int.MaxValue, ErrorMessage = "Must Be Greater Than 0")]
    public int PageNumber { get; set; } = 1;

    [Range(1,MaxPageSize,ErrorMessage = "PageSize Must be between 1 to 30")]
    public int PageSize
    {
        get => _pagesize;
        set => _pagesize = value > MaxPageSize ? MaxPageSize : value ;
    }
}
