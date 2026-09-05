namespace Bread.API.Common.Models.Filtering;

public class TransactionsFilterParameters : BaseFilterParameters
{
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
