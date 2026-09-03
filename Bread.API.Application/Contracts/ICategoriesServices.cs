using Bread.API.Application.Dtos.CategoriesDto;
using Bread.API.Common.Models.Paging;
using Bread.API.Common.Results;

namespace Bread.API.Application.Contracts
{
    public interface ICategoriesServices
    {
        Task<Result<GetCategoryDto>> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
        Task<Result> DeleteCategoryAsync(int categoryId);
        Task<Result<PagedResult<GetCategoriesDto>>> GetCategoriesAsync(
            PaginationParameters paginationParameters);
        Task<Result<GetCategoryDto>> GetCategoryAsync(int categoryId);
        Task<Result> UpdateCategoryAsync(int categoryId, UpdateCategoryDto updateCategoryDto);
    }
}