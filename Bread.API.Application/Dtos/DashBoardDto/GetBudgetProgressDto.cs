namespace Bread.API.Application.Dtos.DashBoardDto;

public record GetBudgetProgressDto(
    int CategoryId,
    string CategoryName,
    decimal BudgetSpent,
    decimal BudgetAmount,
    decimal Remaining,
    decimal PercentageUsed);
