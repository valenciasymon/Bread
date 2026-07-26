using System.ComponentModel.DataAnnotations;

namespace Bread.API.Application.Dtos.TransactionsDto;

public class CreateTransactionDto : IValidatableObject
{
    [Required]
    [Range(typeof(decimal),"0.01","1000000")]
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required]
    [Range(1,int.MaxValue)]
    public int CategoryId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date > DateOnly.FromDateTime(DateTime.Today))
            yield return new ValidationResult(
                "Transaction Date Cannot Be in the Future");
    }
}
