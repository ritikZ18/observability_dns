using Microsoft.EntityFrameworkCore;
using ObservabilityDns.Contracts.DTOs;
using ObservabilityDns.Contracts.Enums;
using ObservabilityDns.Domain.DbContext;
using ObservabilityDns.Domain.Entities;
using DomainEntity = ObservabilityDns.Domain.Entities;

namespace ObservabilityDns.Api.Services;

public class DomainService
{
    private readonly ObservabilityDnsDbContext _dbContext;
    private readonly ILogger<DomainService> _logger;

    public DomainService(ObservabilityDnsDbContext dbContext, ILogger<DomainService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<DomainDto>> GetAllDomainsAsync()
    {
        var domains = await _dbContext.Domains
            .OrderBy(d => d.Name)
            .ToListAsync();

        return domains.Select(d => new DomainDto
        {
            Id = d.Id,
            Name = d.Name,
            Enabled = d.Enabled,
            IntervalMinutes = d.IntervalMinutes,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }

    public async Task<DomainDetailDto?> GetDomainByIdAsync(Guid id)
    {
        var domain = await _dbContext.Domains
            .Include(d => d.Checks)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (domain == null)
            return null;

        var recentRuns = await _dbContext.ProbeRuns
            .Where(pr => pr.DomainId == id)
            .OrderByDescending(pr => pr.CompletedAt)
            .Take(10)
            .ToListAsync();

        var openIncidents = await _dbContext.Incidents
            .Where(i => i.DomainId == id && i.Status == "OPEN")
            .OrderByDescending(i => i.StartedAt)
            .ToListAsync();

        return new DomainDetailDto
        {
            Id = domain.Id,
            Name = domain.Name,
            Enabled = domain.Enabled,
            IntervalMinutes = domain.IntervalMinutes,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt,
            Checks = domain.Checks.Select(c => new CheckDto
            {
                Id = c.Id,
                CheckType = Enum.Parse<CheckType>(c.CheckType),
                Enabled = c.Enabled
            }).ToList(),
            RecentRuns = recentRuns.Select(pr => new ProbeRunDto
            {
                Id = pr.Id,
                DomainId = pr.DomainId,
                DomainName = domain.Name,
                CheckType = Enum.Parse<CheckType>(pr.CheckType),
                Success = pr.Success,
                ErrorCode = pr.ErrorCode,
                ErrorMessage = pr.ErrorMessage,
                DnsMs = pr.DnsMs,
                TlsMs = pr.TlsMs,
                TtfbMs = pr.TtfbMs,
                TotalMs = pr.TotalMs,
                StatusCode = pr.StatusCode,
                RecordsSnapshot = pr.RecordsSnapshot,
                CertificateInfo = pr.CertificateInfo,
                StartedAt = pr.StartedAt,
                CompletedAt = pr.CompletedAt
            }).ToList(),
            OpenIncidents = openIncidents.Select(i => new IncidentDto
            {
                Id = i.Id,
                DomainId = i.DomainId,
                DomainName = domain.Name,
                CheckType = Enum.Parse<CheckType>(i.CheckType),
                Severity = Enum.Parse<Severity>(i.Severity),
                Status = Enum.Parse<IncidentStatus>(i.Status),
                Reason = i.Reason,
                StartedAt = i.StartedAt,
                ResolvedAt = i.ResolvedAt,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList()
        };
    }

    public async Task<DomainDto> CreateDomainAsync(CreateDomainRequest request)
    {
        // Normalize domain name (remove protocol, www, trailing slash)
        var normalizedName = NormalizeDomainName(request.Name);

        var domain = new DomainEntity.Domain
        {
            Name = normalizedName,
            Enabled = request.Enabled,
            IntervalMinutes = request.IntervalMinutes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Domains.Add(domain);
        await _dbContext.SaveChangesAsync();

        // Create default checks for DNS, TLS, HTTP
        var checks = new List<DomainEntity.Check>
        {
            new DomainEntity.Check { DomainId = domain.Id, CheckType = "DNS", Enabled = true },
            new DomainEntity.Check { DomainId = domain.Id, CheckType = "TLS", Enabled = true },
            new DomainEntity.Check { DomainId = domain.Id, CheckType = "HTTP", Enabled = true }
        };

        _dbContext.Checks.AddRange(checks);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created domain {DomainName} with ID {DomainId}", domain.Name, domain.Id);

        return new DomainDto
        {
            Id = domain.Id,
            Name = domain.Name,
            Enabled = domain.Enabled,
            IntervalMinutes = domain.IntervalMinutes,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }

    public async Task<bool> UpdateDomainAsync(Guid id, UpdateDomainRequest request)
    {
        var domain = await _dbContext.Domains.FindAsync(id);
        if (domain == null)
            return false;

        if (request.Enabled.HasValue)
            domain.Enabled = request.Enabled.Value;

        if (request.IntervalMinutes.HasValue)
            domain.IntervalMinutes = request.IntervalMinutes.Value;

        domain.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDomainAsync(Guid id)
    {
        var domain = await _dbContext.Domains.FindAsync(id);
        if (domain == null)
            return false;

        _dbContext.Domains.Remove(domain);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private string NormalizeDomainName(string input)
    {
        var domain = input.Trim().ToLowerInvariant();

        // Remove protocol
        if (domain.StartsWith("http://"))
            domain = domain.Substring(7);
        if (domain.StartsWith("https://"))
            domain = domain.Substring(8);

        // Remove www.
        if (domain.StartsWith("www."))
            domain = domain.Substring(4);

        // Remove trailing slash and path
        var slashIndex = domain.IndexOf('/');
        if (slashIndex >= 0)
            domain = domain.Substring(0, slashIndex);

        // Remove port
        var colonIndex = domain.IndexOf(':');
        if (colonIndex >= 0)
            domain = domain.Substring(0, colonIndex);

        return domain;
    }
}
