using Microsoft.EntityFrameworkCore;
using ObservabilityDns.Contracts.DTOs;
using ObservabilityDns.Domain.DbContext;
using ObservabilityDns.Domain.Entities;
using DomainEntity = ObservabilityDns.Domain.Entities;

namespace ObservabilityDns.Api.Services;

public class GroupService
{
    private readonly ObservabilityDnsDbContext _dbContext;
    private readonly ILogger<GroupService> _logger;

    public GroupService(ObservabilityDnsDbContext dbContext, ILogger<GroupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<GroupDto>> GetAllGroupsAsync()
    {
        var groups = await _dbContext.DomainGroups
            .Include(g => g.Domains)
            .OrderBy(g => g.Name)
            .ToListAsync();

        return groups.Select(g => new GroupDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            Color = g.Color,
            Icon = g.Icon,
            Enabled = g.Enabled,
            DomainCount = g.Domains.Count,
            CreatedAt = g.CreatedAt,
            UpdatedAt = g.UpdatedAt
        }).ToList();
    }

    public async Task<GroupDetailDto?> GetGroupByIdAsync(Guid id)
    {
        var group = await _dbContext.DomainGroups
            .Include(g => g.Domains)
            .ThenInclude(d => d.Checks)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
            return null;

        // Get statistics
        var statistics = await GetGroupStatisticsAsync(id);

        return new GroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Color = group.Color,
            Icon = group.Icon,
            Enabled = group.Enabled,
            DomainCount = group.Domains.Count,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            Domains = group.Domains.Select(d => new DomainDto
            {
                Id = d.Id,
                Name = d.Name,
                Enabled = d.Enabled,
                IntervalMinutes = d.IntervalMinutes,
                GroupId = d.GroupId,
                GroupName = group.Name,
                GroupColor = group.Color,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList(),
            Statistics = statistics
        };
    }

    public async Task<GroupDto> CreateGroupAsync(CreateGroupRequest request)
    {
        // Check if group name already exists
        var existingGroup = await _dbContext.DomainGroups
            .FirstOrDefaultAsync(g => g.Name.ToLower() == request.Name.ToLower());

        if (existingGroup != null)
            throw new InvalidOperationException($"Group with name '{request.Name}' already exists");

        var group = new DomainEntity.DomainGroup
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Color = request.Color?.Trim(),
            Icon = request.Icon?.Trim(),
            Enabled = request.Enabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.DomainGroups.Add(group);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created group {GroupName} with ID {GroupId}", group.Name, group.Id);

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Color = group.Color,
            Icon = group.Icon,
            Enabled = group.Enabled,
            DomainCount = 0,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }

    public async Task<bool> UpdateGroupAsync(Guid id, UpdateGroupRequest request)
    {
        var group = await _dbContext.DomainGroups.FindAsync(id);
        if (group == null)
            return false;

        // Check if new name conflicts with existing group
        if (!string.IsNullOrEmpty(request.Name) && request.Name.Trim().ToLower() != group.Name.ToLower())
        {
            var existingGroup = await _dbContext.DomainGroups
                .FirstOrDefaultAsync(g => g.Name.ToLower() == request.Name.Trim().ToLower() && g.Id != id);

            if (existingGroup != null)
                throw new InvalidOperationException($"Group with name '{request.Name}' already exists");
        }

        if (!string.IsNullOrEmpty(request.Name))
            group.Name = request.Name.Trim();

        if (request.Description != null)
            group.Description = string.IsNullOrEmpty(request.Description) ? null : request.Description.Trim();

        if (request.Color != null)
            group.Color = string.IsNullOrEmpty(request.Color) ? null : request.Color.Trim();

        if (request.Icon != null)
            group.Icon = string.IsNullOrEmpty(request.Icon) ? null : request.Icon.Trim();

        if (request.Enabled.HasValue)
            group.Enabled = request.Enabled.Value;

        group.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteGroupAsync(Guid id, bool unassignDomains = true)
    {
        var group = await _dbContext.DomainGroups
            .Include(g => g.Domains)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
            return false;

        // Unassign domains if requested (otherwise they'll be set to null via FK constraint)
        if (unassignDomains)
        {
            foreach (var domain in group.Domains)
            {
                domain.GroupId = null;
            }
        }

        _dbContext.DomainGroups.Remove(group);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted group {GroupName} with ID {GroupId}", group.Name, group.Id);
        return true;
    }

    public async Task<bool> AssignDomainToGroupAsync(Guid domainId, Guid? groupId)
    {
        var domain = await _dbContext.Domains.FindAsync(domainId);
        if (domain == null)
            return false;

        // Verify group exists if provided
        if (groupId.HasValue)
        {
            var group = await _dbContext.DomainGroups.FindAsync(groupId.Value);
            if (group == null)
                return false;
        }

        domain.GroupId = groupId;
        domain.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<GroupStatisticsDto?> GetGroupStatisticsAsync(Guid groupId)
    {
        var domains = await _dbContext.Domains
            .Where(d => d.GroupId == groupId)
            .Select(d => d.Id)
            .ToListAsync();

        if (domains.Count == 0)
            return new GroupStatisticsDto { TotalDomains = 0 };

        var enabledDomains = await _dbContext.Domains
            .CountAsync(d => d.GroupId == groupId && d.Enabled);

        var probeRuns = await _dbContext.ProbeRuns
            .Where(pr => domains.Contains(pr.DomainId))
            .ToListAsync();

        var totalRuns = probeRuns.Count;
        var successfulRuns = probeRuns.Count(pr => pr.Success);
        var failedRuns = totalRuns - successfulRuns;

        var averageLatency = probeRuns.Any()
            ? probeRuns.Average(pr => pr.TotalMs)
            : 0;

        var openIncidents = await _dbContext.Incidents
            .CountAsync(i => domains.Contains(i.DomainId) && i.Status == "OPEN");

        var lastProbeRun = await _dbContext.ProbeRuns
            .Where(pr => domains.Contains(pr.DomainId))
            .OrderByDescending(pr => pr.CompletedAt)
            .Select(pr => (DateTime?)pr.CompletedAt)
            .FirstOrDefaultAsync();

        return new GroupStatisticsDto
        {
            TotalDomains = domains.Count,
            EnabledDomains = enabledDomains,
            TotalProbeRuns = totalRuns,
            SuccessfulRuns = successfulRuns,
            FailedRuns = failedRuns,
            AverageLatency = Math.Round(averageLatency, 2),
            OpenIncidents = openIncidents,
            LastProbeRun = lastProbeRun
        };
    }
}
