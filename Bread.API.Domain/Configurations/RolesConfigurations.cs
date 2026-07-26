using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bread.API.Domain.Configurations;

public class RolesConfigurations : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(
            new IdentityRole
            {
                Id = "75200d5b-9cd5-44d4-8661-fa28974c48bf",
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "b973dbb2-6c83-4648-a147-e8d42f155cb0"
            },
            new IdentityRole
            {
                Id = "3596d0f9-2a06-48e0-be5a-5f55ba3eb6fd",
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "852c2772-0a8c-4421-aa08-fe5d84cdec9b"
            });
    }
}
