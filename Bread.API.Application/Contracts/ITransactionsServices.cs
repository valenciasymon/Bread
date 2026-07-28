using Bread.API.Application.Dtos.TransactionsDto;
using Bread.API.Common.Results;

namespace Bread.API.Application.Contracts
{
    public interface ITransactionsServices
    {
        Task<Result<GetTransactionsDto>> CreateTransactionAsync(CreateTransactionDto createTransactionDto);
        Task<Result> DeleteTransactionAsync(int transactionId);
        Task<Result<GetTransactionsDto>> GetTransactionAsync(int transactionId);
        Task<Result<IEnumerable<GetTransactionsDto>>> GetTransactionsAsync();
        Task<Result> UpdateTransactionAsync(int transactionId, UpdateTransactionDto updateTransactionDto);
    }
}