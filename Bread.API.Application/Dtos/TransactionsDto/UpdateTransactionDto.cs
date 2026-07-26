namespace Bread.API.Application.Dtos.TransactionsDto;

public class UpdateTransactionDto : CreateTransactionDto
{
    public int TransactionId { get; set; }
}