namespace Bread.API.Application.Dtos.DashBoardDto;

public record GetSpendingDto(
    int CategoryId,
    string CategoryName,
    decimal TotalSpent);