using Bread.API.Application.Dtos.BudgetsDto;
using Bread.API.Common.Results;

namespace Bread.API.Application.Contracts
{
    public interface IBudgetsServices
    {
        Task<Result<GetBudgetsDto>> CreateBudgetAsync(CreateBudgetDto createBudgetDto);
        Task<Result> DeleteBudgetAsync(int budgetId);
        Task<Result<GetBudgetsDto>> GetBudgetAsync(int budgetId);
        Task<Result<IEnumerable<GetBudgetsDto>>> GetBudgetsAsync();
        Task<Result> UpdateBudgetAsync(int budgetId, UpdateBudgetDto updateBudgetDto);
    }
}