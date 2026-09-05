using System.ComponentModel.DataAnnotations;

namespace Bread.API.Common.Models.Filtering;

public class BudgetsFilterParameters : BaseFilterParameters
{
    [Range(1,12,ErrorMessage = "Month Must Be Between 1 to 12")]
    public int? Month { get; set; }
    [Range(1900,2100,ErrorMessage = "Year Must Be Between 1900 to 2100")]
    public int? Year { get; set; }
}
