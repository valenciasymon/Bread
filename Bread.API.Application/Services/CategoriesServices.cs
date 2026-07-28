using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.BudgetsDto;
using Bread.API.Application.Dtos.CategoriesDto;
using Bread.API.Application.Dtos.TransactionsDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.Application.Services;

public class CategoriesServices(
    BreadDbContext context,
    IHttpContextAccessor httpContextAccessor) : ICategoriesServices
{
    private readonly string userId = httpContextAccessor.HttpContext.
        User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;
    public async Task<Result<IEnumerable<GetCategoriesDto>>> GetCategoriesAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<GetCategoriesDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        List<GetCategoriesDto> categories = await context.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new GetCategoriesDto(
                c.CategoryId,
                c.Name,
                c.Type.ToString())).ToListAsync();

        return Result<IEnumerable<GetCategoriesDto>>.Success(categories);
    }

    public async Task<Result<GetCategoryDto>> GetCategoryAsync(int categoryId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetCategoryDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        GetCategoryDto? category = await context.Categories
            .Where(c => c.UserId == userId
            && c.CategoryId == categoryId)
            .Select(c => new GetCategoryDto(
                c.CategoryId,
                c.Name,
                c.Type.ToString(),
                c.Budgets.Select(b => new GetBudgetsDto(
                    b.BudgetsId,
                    b.Amount,
                    b.Month,
                    b.Year,
                    b.CategoryId,
                    b.Category!.Name)).ToList(),
                c.Transactions.Select(t => new GetTransactionsDto(
                    t.TransactionId,
                    t.Amount,
                    t.Date,
                    t.CategoryId,
                    t.Category!.Name,
                    t.CreatedAtUtc)).ToList()))
            .FirstOrDefaultAsync();

        return category == null
            ? Result<GetCategoryDto>.NotFound(new Errors(ErrorCodes.NotFound, "Category Not Found"))
            : Result<GetCategoryDto>.Success(category);
    }

    public async Task<Result<GetCategoryDto>> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetCategoryDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        try
        {
            bool duplicateCategory = await context.Categories.AnyAsync(
                c => c.Name.ToLower().Trim() == createCategoryDto.Name.ToLower().Trim()
                && c.UserId == userId
                && c.Type == createCategoryDto.Type);

            if (duplicateCategory)
                return Result<GetCategoryDto>.Conflict(new Errors(
                    ErrorCodes.Conflict, "This Category Already Exist!"));

            Categories category = new Categories
            {
                Name = createCategoryDto.Name,
                Type = createCategoryDto.Type,
                UserId = userId
            };

            await context.AddAsync(category);
            await context.SaveChangesAsync();

            GetCategoryDto getCategoriesDto = new GetCategoryDto(
                category.CategoryId,
                category.Name,
                category.Type.ToString(),
                [],
                []);

            return Result<GetCategoryDto>.Success(getCategoriesDto);
        }
        catch (Exception)
        {
            return Result<GetCategoryDto>.Failure(new Errors(
                ErrorCodes.Failure, "Unexpected Server Error"));
        }
    }

    public async Task<Result> UpdateCategoryAsync(int categoryId, UpdateCategoryDto updateCategoryDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        if (categoryId != updateCategoryDto.CategoryId)
            return Result.BadRequest(new Errors(
                ErrorCodes.BadRequest, "Route Id Does not Match Payload Id"));
        try
        {
            Categories? category = await context.Categories.FirstOrDefaultAsync(
                c => c.CategoryId == categoryId
                && c.UserId == userId);

            if (category == null)
                return Result.NotFound(new Errors(
                    ErrorCodes.NotFound, "Category Not Found"));

            bool duplicateCategory = await context.Categories.AnyAsync(
                c => c.Name == updateCategoryDto.Name
                && c.UserId == userId
                && c.Type == updateCategoryDto.Type
                && c.CategoryId != categoryId);

            if (duplicateCategory)
                return Result.Conflict(new Errors(
                    ErrorCodes.Conflict, "This Category Already Exist!"));

            category.Name = updateCategoryDto.Name;
            category.Type = updateCategoryDto.Type;

            await context.SaveChangesAsync();

            return Result.Success();

        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CategoryExist(categoryId))
            {
                return Result.NotFound();
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<Result> DeleteCategoryAsync(int categoryId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        int rows = await context.Categories
            .Where(c => c.CategoryId == categoryId
            && c.UserId == userId)
            .ExecuteDeleteAsync();

        if (rows == 0)
            return Result.NotFound(new Errors(
                ErrorCodes.NotFound, "Category Not Found"));

        return Result.Success();
    }


    private async Task<bool> CategoryExist(int categoryId)
    {
        return await context.Categories.AnyAsync(
            c => c.CategoryId == categoryId
            && c.UserId == userId);
    }
}
