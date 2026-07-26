using Bread.API.Application.Dtos.BudgetsDto;
using Bread.API.Domain;

namespace Bread.API.Application.Dtos.CategoriesDto;

public record GetCategoryDto(
    int CategoryId,
    string Name,
    string Type,
    List<GetBudgetsDto> Budgets,
    List<GetBudgetsDto> Transactions);
