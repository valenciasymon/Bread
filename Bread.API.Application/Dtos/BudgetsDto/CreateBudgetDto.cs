using System.ComponentModel.DataAnnotations;

namespace Bread.API.Application.Dtos.BudgetsDto;

public class CreateBudgetDto : IValidatableObject
{
    [Required]
    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Amount { get; set; }
    [Required]
    [Range(1,12,ErrorMessage ="Month Does Not Exist")]
    public int Month { get; set; }
    [Required]
    public int Year { get; set; }
    [Required]
    [Range(1,int.MaxValue)]
    public int CategoryId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if( Year < 1900 || Year > DateTime.Now.Year)
        {
            yield return new ValidationResult(
                $"Year Must be Between 1900 and {DateTime.Now.Year}");
        }
    }
}
