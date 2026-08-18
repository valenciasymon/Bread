namespace Bread.API.Application.Dtos.AdminDto;

public record GetAdminSummaryDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalTransactions,
    int TotalBudgets);