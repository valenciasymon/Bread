namespace Bread.API.Application.Dtos.AdminDto;

public record GetAdminDashBoardDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalTransactions,
    int TotalBudgets);