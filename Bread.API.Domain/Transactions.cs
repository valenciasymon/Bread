using System.ComponentModel.DataAnnotations;

namespace Bread.API.Domain;

public class Transactions
{
    [Key]
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public int CategoryId { get; set; }
    public Categories? Category { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
