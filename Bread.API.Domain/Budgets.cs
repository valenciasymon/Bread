using System.ComponentModel.DataAnnotations;

namespace Bread.API.Domain;

public class Budgets
{
    [Key]
    public int BudgetsId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int CategoryId { get; set; }
    public Categories? Category { get; set; }
    public string UserId { get; set; } = string.Empty;
}
