

using Bread.API.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace Bread.API.Domain;

public class Categories
{
    [Key]
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ICollection<Budgets> Budgets { get; set; } = [];
    public ICollection<Transactions> Transactions { get; set; } = [];
}
