using Bread.API.Application.Dtos.CategoriesDto;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Bread.API.Common.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.Application.Services;

public class CategoriesServices(
    BreadDbContext context,
    IHttpContextAccessor httpContextAccessor)
{
    private string userId = httpContextAccessor.HttpContext.
        User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;
    public async Task<Result<IEnumerable<GetCategoriesDto>>> GetCategoriesAsync()
    {
        if (string.IsNullOrWhiteSpace(userid))
            return Result<IEnumerable<GetCategoriesDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        List<GetCategoriesDto> categories = await context.Categories
            .Where(c => c.UserId == userid)
            .Select(c => new GetCategoriesDto(
                c.CategoryId,
                c.Name,
                c.Type.ToString())).ToListAsync();

        return Result<IEnumerable<GetCategoriesDto>>.Success(categories);
    }
}
