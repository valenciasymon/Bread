namespace Bread.API.Application.Dtos.DashBoardDto;

public class GetDashBoardDto
{
    public required GetSummaryMonthDto Summary { get; set; }
    public required IEnumerable<GetBudgetProgressDto> BudgetProgress { get; set; }
    public required IEnumerable<GetRecentTransactionDto> RecentTransactions { get; set; }
    public required IEnumerable<GetSpendingDto> Spendings { get; set; }
};