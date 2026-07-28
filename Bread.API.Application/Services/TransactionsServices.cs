using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.TransactionsDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.Application.Services;

public class TransactionsServices(
    BreadDbContext context,
    IHttpContextAccessor httpContext) : ITransactionsServices
{
    private readonly string userId = httpContext.HttpContext.User?.
        FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContext.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;
    public async Task<Result<IEnumerable<GetTransactionsDto>>> GetTransactionsAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<GetTransactionsDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        List<GetTransactionsDto> transactions = await context.Transactions
            .Where(t => t.UserId == userId)
            .Select(t => new GetTransactionsDto(
                t.TransactionId,
                t.Amount,
                t.Date,
                t.CategoryId,
                t.Category!.Name,
                t.CreatedAtUtc)).ToListAsync();

        return Result<IEnumerable<GetTransactionsDto>>.Success(transactions);
    }

    public async Task<Result<GetTransactionsDto>> GetTransactionAsync(int transactionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetTransactionsDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        GetTransactionsDto? transaction = await context.Transactions
            .Where(t => t.TransactionId == transactionId && t.UserId == userId)
            .Select(t => new GetTransactionsDto(
                t.TransactionId,
                t.Amount,
                t.Date,
                t.CategoryId,
                t.Category!.Name,
                t.CreatedAtUtc))
            .FirstOrDefaultAsync();

        return transaction == null
            ? Result<GetTransactionsDto>.NotFound(new Errors(
                ErrorCodes.NotFound, "Transaction Not Found"))
            : Result<GetTransactionsDto>.Success(transaction);
    }

    public async Task<Result<GetTransactionsDto>> CreateTransactionAsync(
        CreateTransactionDto createTransactionDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetTransactionsDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        try
        {
            bool categoryExist = await context.Categories.AnyAsync(
                c => c.CategoryId == createTransactionDto.CategoryId
                && c.UserId == userId);

            if (!categoryExist)
                return Result<GetTransactionsDto>.NotFound(new Errors(
                    ErrorCodes.NotFound, "Category Does Not Exist"));

            Transactions transaction = new Transactions
            {
                Amount = createTransactionDto.Amount,
                Date = createTransactionDto.Date,
                CategoryId = createTransactionDto.CategoryId,
                UserId = userId
            };

            await context.AddAsync(transaction);
            await context.SaveChangesAsync();

            await context.Entry(transaction)
                .Reference(t => t.Category)
                .LoadAsync();

            GetTransactionsDto getTransactionsDto = new GetTransactionsDto(
                transaction.TransactionId,
                transaction.Amount,
                transaction.Date,
                transaction.CategoryId,
                transaction.Category!.Name,
                transaction.CreatedAtUtc);

            return Result<GetTransactionsDto>.Success(getTransactionsDto);
        }
        catch (Exception)
        {
            return Result<GetTransactionsDto>.Failure(new Errors(
                ErrorCodes.Failure, "Unexpected Server Error"));
        }
    }

    public async Task<Result> UpdateTransactionAsync(
        int transactionId, UpdateTransactionDto updateTransactionDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        try
        {
            bool categoryExist = await context.Categories.AnyAsync(
                c => c.CategoryId == updateTransactionDto.CategoryId
                && c.UserId == userId);

            if (!categoryExist)
                return Result.NotFound(new Errors(
                    ErrorCodes.NotFound, "Category Not Found"));

            Transactions? transaction = await context.Transactions.FirstOrDefaultAsync(
                t => t.TransactionId == transactionId
                && t.UserId == userId);

            if (transaction == null)
                return Result.NotFound(new Errors(
                    ErrorCodes.NotFound, "Transaction Not Found"));

            transaction.Amount = updateTransactionDto.Amount;
            transaction.Date = updateTransactionDto.Date;
            transaction.CategoryId = updateTransactionDto.CategoryId;

            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.Transactions.AnyAsync(
                t => t.TransactionId == transactionId && t.UserId == userId))
            {
                return Result.NotFound();
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<Result> DeleteTransactionAsync(int transactionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        int rows = await context.Transactions
            .Where(t => t.TransactionId == transactionId && t.UserId == userId)
            .ExecuteDeleteAsync();

        if (rows == 0)
            return Result.NotFound(new Errors(
                ErrorCodes.NotFound, "Transaction Not Found"));

        return Result.Success();
    }
}
