using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bread.API.Domain.Configurations;

public class CategoriesConfigurations : IEntityTypeConfiguration<Categories>
{
    public void Configure(EntityTypeBuilder<Categories> builder)
    {
        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
