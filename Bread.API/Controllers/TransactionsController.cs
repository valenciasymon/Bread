using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.TransactionsDto;
using Bread.API.AuthorizationFilters;
using Bread.API.Common.Models.Filtering;
using Bread.API.Common.Models.Paging;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bread.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireActiveUser]
[Authorize]
public class TransactionsController(ITransactionsServices transactionsServices) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<GetTransactionsDto>>> GetTransactions(
        [FromQuery] PaginationParameters paginationParameters,
        [FromQuery] TransactionsFilterParameters filterParameters)
    {
        Result<PagedResult<GetTransactionsDto>> result = await
            transactionsServices.GetTransactionsAsync(
                paginationParameters,
                filterParameters);

        return ToActionResult(result);
    }

    [HttpGet("{transactionId:int}")]
    public async Task<ActionResult<GetTransactionsDto>> GetTransaction([FromRoute]int transactionId)
    {
        Result<GetTransactionsDto> result = await transactionsServices.GetTransactionAsync(transactionId);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<GetTransactionsDto>> CreateTransaction(
        [FromBody]CreateTransactionDto createTransactionDto)
    {
        Result<GetTransactionsDto> result = await transactionsServices
            .CreateTransactionAsync(createTransactionDto);

        if (!result.Succeeded) return MapErrorToActionResult(result.Errors);

        return CreatedAtAction(
            nameof(GetTransaction),
            new { transactionId = result.Value!.TransactionId}, 
            result.Value);
    }

    [HttpPut("{transactionId:int}")]
    public async Task<ActionResult> UpdateTransaction(
       [FromRoute] int transactionId,
       [FromBody] UpdateTransactionDto updateTransactionDto)
    {
        Result result = await transactionsServices
            .UpdateTransactionAsync(transactionId, updateTransactionDto);

        return ToActionResult(result);
    }

    [HttpDelete("{transactionId:int}")]
    public async Task<ActionResult> DeleteTransaction([FromRoute]int transactionId)
    {
        Result result = await transactionsServices.DeleteTransactionAsync(transactionId);

        return ToActionResult(result);
    }
}
