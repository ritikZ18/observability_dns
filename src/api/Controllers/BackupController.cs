using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObservabilityDns.Domain.DbContext;
using System.Text.Json;

namespace ObservabilityDns.Api.Controllers;

[ApiController]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    private readonly ObservabilityDnsDbContext _dbContext;
    private readonly ILogger<BackupController> _logger;

    public BackupController(ObservabilityDnsDbContext dbContext, ILogger<BackupController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        try
        {
            // Export all data
            var backup = new
            {
                exportedAt = DateTime.UtcNow,
                version = "1.0",
                groups = await _dbContext.DomainGroups
                    .Include(g => g.Domains)
                    .ThenInclude(d => d.Checks)
                    .ToListAsync(),
                domains = await _dbContext.Domains
                    .Include(d => d.Checks)
                    .Where(d => d.GroupId == null) // Domains without groups (already included in groups)
                    .ToListAsync()
            };

            var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var fileName = $"observability-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";

            return File(
                System.Text.Encoding.UTF8.GetBytes(json),
                "application/json",
                fileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting backup");
            return StatusCode(500, new { error = "Failed to export backup", message = ex.Message });
        }
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportBackupRequest request)
    {
        try
        {
            if (request.ClearExisting)
            {
                // Clear existing data
                _dbContext.ProbeRuns.RemoveRange(await _dbContext.ProbeRuns.ToListAsync());
                _dbContext.Incidents.RemoveRange(await _dbContext.Incidents.ToListAsync());
                _dbContext.AlertRules.RemoveRange(await _dbContext.AlertRules.ToListAsync());
                _dbContext.Notifications.RemoveRange(await _dbContext.Notifications.ToListAsync());
                _dbContext.Checks.RemoveRange(await _dbContext.Checks.ToListAsync());
                _dbContext.Domains.RemoveRange(await _dbContext.Domains.ToListAsync());
                _dbContext.DomainGroups.RemoveRange(await _dbContext.DomainGroups.ToListAsync());
                await _dbContext.SaveChangesAsync();
            }

            // Import groups
            if (request.BackupData.Groups != null)
            {
                foreach (var groupData in request.BackupData.Groups)
                {
                    // Check if group already exists
                    var existingGroup = await _dbContext.DomainGroups.FindAsync(groupData.Id);
                    if (existingGroup != null && request.ClearExisting)
                    {
                        _dbContext.DomainGroups.Remove(existingGroup);
                        await _dbContext.SaveChangesAsync();
                    }
                    else if (existingGroup != null)
                    {
                        // Update existing group instead of creating duplicate
                        existingGroup.Name = groupData.Name;
                        existingGroup.Description = groupData.Description;
                        existingGroup.Color = groupData.Color;
                        existingGroup.Icon = groupData.Icon;
                        existingGroup.Enabled = groupData.Enabled;
                        existingGroup.UpdatedAt = DateTime.UtcNow;
                        continue; // Skip to next group
                    }

                    var group = new ObservabilityDns.Domain.Entities.DomainGroup
                    {
                        Id = Guid.NewGuid(), // Generate new ID to avoid conflicts
                        Name = groupData.Name,
                        Description = groupData.Description,
                        Color = groupData.Color,
                        Icon = groupData.Icon,
                        Enabled = groupData.Enabled,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _dbContext.DomainGroups.Add(group);
                    
                    // Save to get the new group ID
                    await _dbContext.SaveChangesAsync();

                    // Import domains in this group
                    if (groupData.Domains != null)
                    {
                        foreach (var domainData in groupData.Domains)
                        {
                            // Check if domain already exists
                            var existingDomain = await _dbContext.Domains.FindAsync(domainData.Id);
                            if (existingDomain != null && request.ClearExisting)
                            {
                                _dbContext.Domains.Remove(existingDomain);
                                await _dbContext.SaveChangesAsync();
                            }
                            else if (existingDomain != null)
                            {
                                // Update existing domain
                                existingDomain.Name = domainData.Name;
                                existingDomain.Enabled = domainData.Enabled;
                                existingDomain.IntervalMinutes = domainData.IntervalMinutes;
                                existingDomain.Icon = domainData.Icon;
                                existingDomain.GroupId = group.Id;
                                existingDomain.UpdatedAt = DateTime.UtcNow;
                                continue; // Skip to next domain
                            }

                            var domain = new ObservabilityDns.Domain.Entities.Domain
                            {
                                Id = Guid.NewGuid(), // Generate new ID
                                Name = domainData.Name,
                                Enabled = domainData.Enabled,
                                IntervalMinutes = domainData.IntervalMinutes,
                                Icon = domainData.Icon,
                                GroupId = group.Id,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            _dbContext.Domains.Add(domain);
                            await _dbContext.SaveChangesAsync(); // Save to get domain ID

                            // Import checks
                            if (domainData.Checks != null)
                            {
                                foreach (var checkData in domainData.Checks)
                                {
                                    var check = new ObservabilityDns.Domain.Entities.Check
                                    {
                                        Id = Guid.NewGuid(), // Generate new ID
                                        DomainId = domain.Id,
                                        CheckType = checkData.CheckType,
                                        Enabled = checkData.Enabled
                                    };
                                    _dbContext.Checks.Add(check);
                                }
                            }
                        }
                    }
                }
            }

            // Import domains without groups
            if (request.BackupData.Domains != null)
            {
                foreach (var domainData in request.BackupData.Domains)
                {
                    // Check if domain already exists
                    var existingDomain = await _dbContext.Domains.FindAsync(domainData.Id);
                    if (existingDomain != null && request.ClearExisting)
                    {
                        _dbContext.Domains.Remove(existingDomain);
                        await _dbContext.SaveChangesAsync();
                    }
                    else if (existingDomain != null)
                    {
                        // Update existing domain
                        existingDomain.Name = domainData.Name;
                        existingDomain.Enabled = domainData.Enabled;
                        existingDomain.IntervalMinutes = domainData.IntervalMinutes;
                        existingDomain.Icon = domainData.Icon;
                        existingDomain.UpdatedAt = DateTime.UtcNow;
                        continue; // Skip to next domain
                    }

                    var domain = new ObservabilityDns.Domain.Entities.Domain
                    {
                        Id = Guid.NewGuid(), // Generate new ID
                        Name = domainData.Name,
                        Enabled = domainData.Enabled,
                        IntervalMinutes = domainData.IntervalMinutes,
                        Icon = domainData.Icon,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _dbContext.Domains.Add(domain);
                    await _dbContext.SaveChangesAsync(); // Save to get domain ID

                    // Import checks
                    if (domainData.Checks != null)
                    {
                        foreach (var checkData in domainData.Checks)
                        {
                            var check = new ObservabilityDns.Domain.Entities.Check
                            {
                                Id = Guid.NewGuid(), // Generate new ID
                                DomainId = domain.Id,
                                CheckType = checkData.CheckType,
                                Enabled = checkData.Enabled
                            };
                            _dbContext.Checks.Add(check);
                        }
                    }
                }
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Imported backup with {GroupCount} groups and {DomainCount} domains",
                request.BackupData.Groups?.Count ?? 0,
                (request.BackupData.Groups?.Sum(g => g.Domains?.Count ?? 0) ?? 0) + (request.BackupData.Domains?.Count ?? 0));

            return Ok(new { message = "Backup imported successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing backup");
            return StatusCode(500, new { error = "Failed to import backup", message = ex.Message });
        }
    }
}

public class ImportBackupRequest
{
    public BackupData BackupData { get; set; } = null!;
    public bool ClearExisting { get; set; } = false;
}

public class BackupData
{
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<GroupBackup>? Groups { get; set; }
    public List<DomainBackup>? Domains { get; set; }
}

public class GroupBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DomainBackup>? Domains { get; set; }
}

public class DomainBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; }
    public string? Icon { get; set; }
    public Guid? GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CheckBackup>? Checks { get; set; }
}

public class CheckBackup
{
    public Guid Id { get; set; }
    public string CheckType { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
