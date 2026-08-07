namespace Bread.API.Application.Dtos.DashBoardDto;

public record GetRecentTransactionDto(
    int CategoryId,
    string CategoryName,
    int TransactionId,
    decimal TransactionAmount,
    string Type,
    DateOnly Date);
