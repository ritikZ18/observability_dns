using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ObservabilityDns.Domain.DbContext;

// Design-time factory for EF Core tools (migrations, database update).
public class ObservabilityDnsDbContextFactory
    : IDesignTimeDbContextFactory<ObservabilityDnsDbContext>
{
    public ObservabilityDnsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ObservabilityDnsDbContext>();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            "Host=localhost;Port=5432;Database=observability_dns;Username=observability;Password=observability_dev";

        optionsBuilder.UseNpgsql(connectionString);

        return new ObservabilityDnsDbContext(optionsBuilder.Options);
    }
}
