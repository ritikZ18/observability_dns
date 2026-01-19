using Microsoft.EntityFrameworkCore;
using ObservabilityDns.Domain.Entities;

namespace ObservabilityDns.Domain.DbContext;

public class ObservabilityDnsDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ObservabilityDnsDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<ObservabilityDnsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Entities.Domain> Domains { get; set; }
    public DbSet<Entities.Check> Checks { get; set; }
    public DbSet<Entities.ProbeRun> ProbeRuns { get; set; }
    public DbSet<Entities.Incident> Incidents { get; set; }
    public DbSet<Entities.AlertRule> AlertRules { get; set; }
    public DbSet<Entities.Notification> Notifications { get; set; }
    public DbSet<Entities.NotificationAttempt> NotificationAttempts { get; set; }

    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Domain configuration
        modelBuilder.Entity<Entities.Domain>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Enabled);
        });

        // Check configuration
        modelBuilder.Entity<Entities.Check>(entity =>
        {
            entity.HasIndex(e => new { e.DomainId, e.CheckType }).IsUnique();
            entity.HasOne(e => e.Domain)
                .WithMany(d => d.Checks)
                .HasForeignKey(e => e.DomainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProbeRun configuration
        modelBuilder.Entity<Entities.ProbeRun>(entity =>
        {
            entity.HasIndex(e => e.DomainId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Domain)
                .WithMany(d => d.ProbeRuns)
                .HasForeignKey(e => e.DomainId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Check)
                .WithMany(c => c.ProbeRuns)
                .HasForeignKey(e => e.CheckId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Incident configuration
        modelBuilder.Entity<Entities.Incident>(entity =>
        {
            entity.HasIndex(e => e.DomainId);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Domain)
                .WithMany(d => d.Incidents)
                .HasForeignKey(e => e.DomainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AlertRule configuration
        modelBuilder.Entity<Entities.AlertRule>(entity =>
        {
            entity.HasOne(e => e.Domain)
                .WithMany(d => d.AlertRules)
                .HasForeignKey(e => e.DomainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification configuration
        modelBuilder.Entity<Entities.Notification>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Domain)
                .WithMany(d => d.Notifications)
                .HasForeignKey(e => e.DomainId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Incident)
                .WithMany(i => i.Notifications)
                .HasForeignKey(e => e.IncidentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // NotificationAttempt configuration
        modelBuilder.Entity<Entities.NotificationAttempt>(entity =>
        {
            entity.HasOne(e => e.Notification)
                .WithMany(n => n.NotificationAttempts)
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
