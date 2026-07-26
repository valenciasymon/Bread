namespace Bread.API.Application.Dtos.CategoriesDto;

public record GetCategoriesDto(
    int CategoryId,
    string Name,
    string Type);
