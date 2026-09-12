using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.CategoriesDto;
using Bread.API.AuthorizationFilters;
using Bread.API.CachePolicy;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Filtering;
using Bread.API.Common.Models.Paging;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace Bread.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireActiveUser]
[ValidateSecurityStamp]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.FixedPolicy)]
public class CategoriesController(ICategoriesServices categoriesServices) : BaseApiController
{
    [HttpGet]
    [OutputCache(PolicyName = CacheConstants.AuthenticatedUserCachingPolicy)]
    public async Task<ActionResult<PagedResult<GetCategoriesDto>>> GetCategories(
        [FromQuery] PaginationParameters paginationParameters,
        [FromQuery] CategoriesFilterParameters filterParameters)
    {
        Result<PagedResult<GetCategoriesDto>> result = await
           categoriesServices.GetCategoriesAsync(paginationParameters, filterParameters);

        return ToActionResult<PagedResult<GetCategoriesDto>>(result);
    }

    [HttpGet("{categoryId:int}")]
    public async Task<ActionResult<GetCategoryDto>> GetCategory([FromRoute] int categoryId)
    {
        Result<GetCategoryDto> result = await categoriesServices.GetCategoryAsync(categoryId);

        return ToActionResult<GetCategoryDto>(result);
    }

    [HttpPost()]
    public async Task<ActionResult<GetCategoryDto>> CreateCategory(
        [FromBody] CreateCategoryDto createCategoryDto)
    {
        Result<GetCategoryDto> result = await categoriesServices.CreateCategoryAsync(createCategoryDto);

        if (!result.Succeeded)
            return MapErrorToActionResult(result.Errors);

        return CreatedAtAction(
            nameof(GetCategory),
            new { categoryId = result.Value!.CategoryId },
            result.Value);
    }

    [HttpPut("{categoryId:int}")]
    public async Task<ActionResult> UpdateCategory(
        [FromRoute] int categoryId,
        [FromBody] UpdateCategoryDto updateCategoryDto)
    {
        Result result = await categoriesServices.UpdateCategoryAsync(categoryId, updateCategoryDto);

        return ToActionResult(result);
    }

    [HttpDelete("{categoryId:int}")]
    public async Task<ActionResult> DeleteCategory(int categoryId)
    {
        Result result = await categoriesServices.DeleteCategoryAsync(categoryId);

        return ToActionResult(result);
    }
}
