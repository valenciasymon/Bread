using Bread.API.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace Bread.API.Application.Dtos.CategoriesDto;

public class CreateCategoryDto
{
    [Required]
    [StringLength(30,MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    [Required]
    public CategoryType Type { get; set; }
}
