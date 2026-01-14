using Microsoft.EntityFrameworkCore;

namespace ObservabilityDns.Domain.DbContext;

// Minimal DbContext scaffold for EF Core tooling.
// Entities and DbSet<>s will be added in Phase 1.
public class ObservabilityDnsDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ObservabilityDnsDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<ObservabilityDnsDbContext> options)
        : base(options)
    {
    }

    // TODO: Add DbSet<> properties for entities (Domain, Check, ProbeRun, etc.)

    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        // TODO: Configure entity relationships and constraints.
        base.OnModelCreating(modelBuilder);
    }
}
