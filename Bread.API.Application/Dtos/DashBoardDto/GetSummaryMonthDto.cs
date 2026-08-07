namespace Bread.API.Application.Dtos.DashBoardDto;

public record GetSummaryMonthDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal CurrentBalance);
