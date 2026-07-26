namespace Bread.API.Application.Dtos.BudgetsDto;

public record GetBudgetsDto(
    int BudgetsId,
    decimal Amount,
    int Month,
    int Year,
    int CategoryId,
    string CategoryName);
