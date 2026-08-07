using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.DashBoardDto;
using Bread.API.Application.Dtos.TransactionsDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Bread.API.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.Application.Services;

public class DashBoardServices(
    BreadDbContext context,
    IHttpContextAccessor httpContext) : IDashBoardServices
{
    private readonly string userId =
        httpContext.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;

    private async Task<Result<GetSummaryMonthDto>> GetSummaryAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetSummaryMonthDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        decimal totalIncome = await context.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == userId
            && b.Month == currentDate.Month
            && b.Year == currentDate.Year
            && b.Category!.Type == CategoryType.Income)
            .SumAsync(b => b.Amount);

        decimal totalExpense = await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
            && t.Date.Month == currentDate.Month
            && t.Date.Year == currentDate.Year
            && t.Category!.Type == CategoryType.Expense)
            .SumAsync(t => t.Amount);

        decimal currentBalance = totalIncome - totalExpense;

        GetSummaryMonthDto summary = new GetSummaryMonthDto(
            totalIncome,
            totalExpense,
            currentBalance);

        return Result<GetSummaryMonthDto>.Success(summary);
    }

    private async Task<Result<IEnumerable<GetBudgetProgressDto>>> GetBudgetProgressAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<GetBudgetProgressDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        List<GetBudgetProgressDto> budgetProgress = await context.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == userId
            && b.Month == currentDate.Month
            && b.Year == currentDate.Year
            && b.Category!.Type == CategoryType.Expense)
            .Select(b => new
            {
                b.CategoryId,
                CategoryName = b.Category!.Name,
                BudgetAmount = b.Amount,
                BudgetSpent = b.Category.Transactions.Where(
                    t => t.UserId == userId
                    && t.Date.Month == currentDate.Month
                    && t.Date.Year == currentDate.Year).Sum(t => t.Amount)
            })
            .Select(a => new GetBudgetProgressDto(
                a.CategoryId,
                a.CategoryName,
                a.BudgetSpent,
                a.BudgetAmount,
                a.BudgetAmount - a.BudgetSpent,
                a.BudgetSpent / a.BudgetAmount * 100))
            .ToListAsync();

        return Result<IEnumerable<GetBudgetProgressDto>>.Success(budgetProgress);
    }

    private async Task<Result<IEnumerable<GetRecentTransactionDto>>> GetRecentTransactionsAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<GetRecentTransactionDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        var recentTransactions = await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
            && t.Date.Month == currentDate.Month
            && t.Date.Year == currentDate.Year)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Take(6)
            .Select(t => new GetRecentTransactionDto(
                t.CategoryId,
                t.Category!.Name,
                t.TransactionId,
                t.Amount,
                t.Category.Type.ToString(),
                t.Date)).ToListAsync();

        return Result<IEnumerable<GetRecentTransactionDto>>.Success(recentTransactions);
    }

    private async Task<Result<IEnumerable<GetSpendingDto>>> GetSpendingCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<GetSpendingDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        List<GetSpendingDto> totalSpent = await context.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId
            && c.Type == CategoryType.Expense)
            .Select(c => new GetSpendingDto(
                c.CategoryId,
                c.Name,
                c.Transactions.Where(
                    t => t.UserId == userId
                    && t.Date.Month == currentDate.Month
                    && t.Date.Year == currentDate.Year).Sum(t => t.Amount)
                )).ToListAsync();

        return Result<IEnumerable<GetSpendingDto>>.Success(totalSpent);
    }

    public async Task<Result<GetDashBoardDto>> GetDashBoardAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetDashBoardDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        Result<GetSummaryMonthDto> summary = await GetSummaryAsync();
        Result<IEnumerable<GetBudgetProgressDto>> budgetProgress = await GetBudgetProgressAsync();
        Result<IEnumerable<GetRecentTransactionDto>> transactions = await GetRecentTransactionsAsync();
        Result<IEnumerable<GetSpendingDto>> totalSpent = await GetSpendingCategoryAsync();

        if (!summary.Succeeded)
            return Result<GetDashBoardDto>.Failure(summary.Errors);

        if (!budgetProgress.Succeeded)
            return Result<GetDashBoardDto>.Failure(budgetProgress.Errors);

        if (!transactions.Succeeded)
            return Result<GetDashBoardDto>.Failure(transactions.Errors);

        if (!totalSpent.Succeeded)
            return Result<GetDashBoardDto>.Failure(totalSpent.Errors);

        GetDashBoardDto dashBoardDto = new GetDashBoardDto
        {
            Summary = summary.Value!,
            BudgetProgress = budgetProgress.Value!,
            RecentTransactions = transactions.Value!,
            Spendings = totalSpent.Value!
        };

        return Result<GetDashBoardDto>.Success(dashBoardDto);
    }
}
