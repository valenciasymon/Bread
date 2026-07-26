using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bread.API.Domain.Configurations
{
    public class TransactionsConfigurations : IEntityTypeConfiguration<Transactions>
    {
        public void Configure(EntityTypeBuilder<Transactions> builder)
        {
            builder.Property(t => t.Amount)
                .HasPrecision(18, 2);

            builder.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
