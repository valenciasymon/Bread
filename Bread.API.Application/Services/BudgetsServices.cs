using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.BudgetsDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Bread.API.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.Application.Services;

public class BudgetsServices(
    BreadDbContext context,
    IHttpContextAccessor httpContext) : IBudgetsServices
{
    private readonly string userId = httpContext.HttpContext?.
        User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
        ?? string.Empty;

    public async Task<Result<IEnumerable<GetBudgetsDto>>> GetBudgetsAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<GetBudgetsDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        List<GetBudgetsDto> budgets = await context.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => new GetBudgetsDto(
                b.BudgetsId,
                b.Amount,
                b.Month,
                b.Year,
                b.CategoryId,
                b.Category!.Name)).ToListAsync();

        return Result<IEnumerable<GetBudgetsDto>>.Success(budgets);
    }

    public async Task<Result<GetBudgetsDto>> GetBudgetAsync(int budgetId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetBudgetsDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        GetBudgetsDto? budget = await context.Budgets
            .AsNoTracking()
            .Where(b => b.BudgetsId == budgetId
            && b.UserId == userId)
            .Select(b => new GetBudgetsDto(
                b.BudgetsId,
                b.Amount,
                b.Month,
                b.Year,
                b.CategoryId,
                b.Category!.Name))
            .FirstOrDefaultAsync();

        return budget == null
            ? Result<GetBudgetsDto>.NotFound(new Errors(ErrorCodes.NotFound, "Budget Not Found"))
            : Result<GetBudgetsDto>.Success(budget);
    }

    public async Task<Result<GetBudgetsDto>> CreateBudgetAsync(CreateBudgetDto createBudgetDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetBudgetsDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId Is Required"));

        try
        {
            bool categoryExist = await context.Categories
                .AnyAsync(c => c.CategoryId == createBudgetDto.CategoryId
                && c.UserId == userId);

            if (!categoryExist)
                return Result<GetBudgetsDto>.NotFound(new Errors(
                    ErrorCodes.NotFound, "Category Not Found"));

            bool duplicateBudget = await context.Budgets
                .AnyAsync(b => b.UserId == userId
                && b.CategoryId == createBudgetDto.CategoryId
                && b.Month == createBudgetDto.Month
                && b.Year == createBudgetDto.Year);

            if (duplicateBudget)
                return Result<GetBudgetsDto>.Conflict(new Errors(
                    ErrorCodes.Conflict, "Budget Already Exist!"));

            DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

            var totals = await context.Budgets
                .AsNoTracking()
                .Where(b => b.UserId == userId
                && b.Month == currentDate.Month
                && b.Year == currentDate.Year)
                .Select(b => new
                {
                    b.Amount,
                    b.Category!.Type
                })
                .ToListAsync();

            decimal totalIncome = totals.Where(b => b.Type == CategoryType.Income)
                .Sum(b => b.Amount);

            decimal currentBudgets = totals.Where(b => b.Type == CategoryType.Expense)
                .Sum(b => b.Amount);

            decimal currentBalance = totalIncome - currentBudgets;

            if (currentBalance < createBudgetDto.Amount)
                return Result<GetBudgetsDto>.Conflict(new Errors(
                    ErrorCodes.Conflict, "The Amount Exceeds The Current Income Balance"));
           
            Budgets budget = new Budgets
            {
                Amount = createBudgetDto.Amount,
                Month = createBudgetDto.Month,
                Year = createBudgetDto.Year,
                CategoryId = createBudgetDto.CategoryId,
                UserId = userId
            };

            await context.AddAsync(budget);
            await context.SaveChangesAsync();

            await context.Entry(budget)
                .Reference(b => b.Category)
                .LoadAsync();

            GetBudgetsDto getBudgetsDto = new(
                budget.BudgetsId,
                budget.Amount,
                budget.Month,
                budget.Year,
                budget.CategoryId,
                budget.Category!.Name);

            return Result<GetBudgetsDto>.Success(getBudgetsDto);
        }
        catch (Exception)
        {
            return Result<GetBudgetsDto>.Failure(new Errors(
                ErrorCodes.Failure, "Unexpected Server Error"));
        }
    }

    public async Task<Result> UpdateBudgetAsync(
        int budgetId, UpdateBudgetDto updateBudgetDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        if (budgetId != updateBudgetDto.BudgetId)
            return Result.Validation(new Errors(
                ErrorCodes.Validation, "RouteId Does Not Match Payload Id"));

        try
        {
            bool categoryExist = await context.Categories
                .AnyAsync(c => c.CategoryId == updateBudgetDto.CategoryId
                && c.UserId == userId);

            if (!categoryExist)
                return Result.NotFound(new Errors(
                    ErrorCodes.NotFound, "Category Not Found"));

            bool duplicateBudget = await context.Budgets
                .AnyAsync(b => b.UserId == userId
                && b.CategoryId == updateBudgetDto.CategoryId
                && b.Month == updateBudgetDto.Month
                && b.Year == updateBudgetDto.Year
                && b.BudgetsId != updateBudgetDto.BudgetId);

            if (duplicateBudget)
                return Result.Conflict(new Errors(
                    ErrorCodes.Conflict, "Budget Already Exist!"));

            Budgets? budget = await context.Budgets
                .FirstOrDefaultAsync(b => b.BudgetsId == updateBudgetDto.BudgetId
                && b.UserId == userId);

            if (budget == null)
                return Result.NotFound(new Errors(
                    ErrorCodes.NotFound, "Budget Not Found"));

            budget.Amount = updateBudgetDto.Amount;
            budget.Month = updateBudgetDto.Month;
            budget.Year = updateBudgetDto.Year;
            budget.CategoryId = updateBudgetDto.CategoryId;

            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.Budgets.AnyAsync(b => b.BudgetsId == updateBudgetDto.BudgetId
            && b.UserId == userId))
            {
                return Result.NotFound(new Errors(
                    ErrorCodes.NotFound, "Budget Not Found"));
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<Result> DeleteBudgetAsync(int budgetId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId Is Required"));

        int rows = await context.Budgets
            .Where(b => b.BudgetsId == budgetId
            && b.UserId == userId)
            .ExecuteDeleteAsync();

        if (rows == 0)
            return Result.NotFound(new Errors(
                ErrorCodes.NotFound, "Budget Not Found"));

        return Result.Success();
    }
}
