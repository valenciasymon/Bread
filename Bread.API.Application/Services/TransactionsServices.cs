using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.TransactionsDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Extensions;
using Bread.API.Common.Models.Filtering;
using Bread.API.Common.Models.Paging;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Bread.API.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.Application.Services;

public class TransactionsServices(
    BreadDbContext context,
    IHttpContextAccessor httpContext) : ITransactionsServices
{
    private readonly string userId = httpContext.HttpContext?.User?.
        FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;
    public async Task<Result<PagedResult<GetTransactionsDto>>> GetTransactionsAsync(
        PaginationParameters paginationParameters,
        TransactionsFilterParameters filterParameters)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<PagedResult<GetTransactionsDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        IQueryable<Transactions> query = context.Transactions
            .Where(t => t.UserId == userId);

        if (filterParameters.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= filterParameters.MinAmount);

        if (filterParameters.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= filterParameters.MaxAmount);

        if (filterParameters.DateFrom.HasValue)
            query = query.Where(t => t.Date >= filterParameters.DateFrom);

        if (filterParameters.DateTo.HasValue)
            query = query.Where(t => t.Date <= filterParameters.DateTo);


        query = filterParameters.SortBy?.Trim().ToLowerInvariant() switch
        {
            "amount" => filterParameters.SortDescending
                ? query.OrderByDescending(t => t.Amount)
                : query.OrderBy(t => t.Amount),
            "date" => filterParameters.SortDescending
                ? query.OrderByDescending(t => t.Amount)
                : query.OrderBy(t => t.Amount),
            _ => query.OrderBy(t => t.Date)
        };

        PagedResult<GetTransactionsDto> transactions = await query
            .AsNoTracking()
            .Select(t => new GetTransactionsDto(
                t.TransactionId,
                t.Amount,
                t.Date,
                t.CategoryId,
                t.Category!.Name,
                t.CreatedAtUtc))
            .ToPagedResult<GetTransactionsDto>(paginationParameters);

        return Result<PagedResult<GetTransactionsDto>>.Success(transactions);
    }

    public async Task<Result<GetTransactionsDto>> GetTransactionAsync(int transactionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetTransactionsDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        GetTransactionsDto? transaction = await context.Transactions
            .AsNoTracking()
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
            Categories? category = await context.Categories.FirstOrDefaultAsync(
                c => c.CategoryId == createTransactionDto.CategoryId
                && c.UserId == userId);

            if (category == null)
                return Result<GetTransactionsDto>.NotFound(new Errors(
                    ErrorCodes.NotFound, "Category Does Not Exist"));

            if (category.Type == CategoryType.Income)
                return Result<GetTransactionsDto>.BadRequest(new Errors(
                    ErrorCodes.BadRequest,
                    "Transactions can only be created for expense categories."));

            Transactions transaction = new Transactions
            {
                Amount = createTransactionDto.Amount,
                Date = createTransactionDto.Date,
                CategoryId = createTransactionDto.CategoryId,
                UserId = userId
            };

            AuditLog auditLog = new AuditLog
            {
                UserId = userId,
                Action = AuditActions.Created,
                Details = $"{AuditDetails.Transactions}{transaction.Amount}"
            };

            await context.Transactions.AddAsync(transaction);
            await context.AuditLogs.AddAsync(auditLog);
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

        if (transactionId != updateTransactionDto.TransactionId)
            return Result.Validation(new Errors(
                ErrorCodes.Validation, "RouteId Does Not Match PayloadId"));

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

            AuditLog auditLog = new AuditLog
            {
                UserId = userId,
                Action = AuditActions.Updated,
                Details = $"{AuditDetails.Transactions}ID:{transaction.TransactionId}"
            };

            await context.AuditLogs.AddAsync(auditLog);
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
        try
        {
            Transactions? transaction = await context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId
                && t.UserId == userId);

            if (transaction == null)
                return Result.NotFound(new Errors(
                    ErrorCodes.NotFound, "Transaction Not Found"));

            AuditLog auditLog = new AuditLog
            {
                UserId = userId,
                Action = AuditActions.Deleted,
                Details = $"{AuditDetails.Transactions}ID:{transaction.TransactionId}"
            };

            context.Transactions.Remove(transaction);
            await context.AuditLogs.AddAsync(auditLog);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(new Errors(
                ErrorCodes.Failure, "Unexpected Server Error"));
        }

    }
}
