using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.BudgetsDto;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bread.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController(IBudgetsServices budgetsServices) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetBudgetsDto>>> GetBudgets()
    {
        Result<IEnumerable<GetBudgetsDto>> result = await budgetsServices.GetBudgetsAsync();

        return ToActionResult(result);
    }

    [HttpGet("{budgetId:int}")]
    public async Task<ActionResult<GetBudgetsDto>> GetBudget([FromRoute] int budgetId)
    {
        Result<GetBudgetsDto> result = await budgetsServices.GetBudgetAsync(budgetId);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<GetBudgetsDto>> CreateBudget(
        [FromBody] CreateBudgetDto createBudgetDto)
    {
        Result<GetBudgetsDto> result = await budgetsServices.CreateBudgetAsync(createBudgetDto);

        if (!result.Succeeded)
            return MapErrorToActionResult(result.Errors);

        return CreatedAtAction(
            nameof(GetBudget),
            new { budgetId = result.Value!.BudgetsId },
            result.Value);
    }

    [HttpPut("{budgetId:int}")]
    public async Task<ActionResult> UpdateBudget(
        [FromRoute] int budgetId,
        [FromBody] UpdateBudgetDto updateBudgetDto)
    {
        Result result = await budgetsServices.UpdateBudgetAsync(budgetId, updateBudgetDto);

        return ToActionResult(result);
    }

    [HttpDelete("{budgetId:int}")]
    public async Task<ActionResult> DeleteBudget([FromRoute]int budgetId)
    {
        Result result = await budgetsServices.DeleteBudgetAsync(budgetId);

        return ToActionResult(result);
    }
}
