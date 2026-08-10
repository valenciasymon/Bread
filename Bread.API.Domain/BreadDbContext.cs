using Bread.API.Domain.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Bread.API.Domain;

public class BreadDbContext : DbContext
{
    public DbSet<Categories> Categories { get; set; }
    public DbSet<Budgets> Budgets { get; set; }
    public DbSet<Transactions> Transactions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public BreadDbContext(DbContextOptions<BreadDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {

        builder.ApplyConfiguration(new BudgetsConfigurations());
        builder.ApplyConfiguration(new TransactionsConfigurations());
        builder.ApplyConfiguration(new CategoriesConfigurations());
    }
}
