using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bread.API.Domain.Configurations;

public class BudgetsConfigurations : IEntityTypeConfiguration<Budgets>
{
    public void Configure(EntityTypeBuilder<Budgets> builder)
    {
        builder.Property(b => b.Amount)
            .HasPrecision(18, 2);

        builder.HasOne(b => b.Category)
            .WithMany(c => c.Budgets)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
