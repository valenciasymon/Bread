namespace Bread.API.Application.Dtos.TransactionsDto;

public record GetTransactionsDto(
    int TransactionId,
    decimal Amount,
    DateOnly Date,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAtUtc);
