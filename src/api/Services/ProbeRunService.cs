using Microsoft.EntityFrameworkCore;
using ObservabilityDns.Contracts.DTOs;
using ObservabilityDns.Contracts.Enums;
using ObservabilityDns.Domain.DbContext;

namespace ObservabilityDns.Api.Services;

public class ProbeRunService
{
    private readonly ObservabilityDnsDbContext _dbContext;

    public ProbeRunService(ObservabilityDnsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ProbeRunDto>> GetProbeRunsByDomainIdAsync(Guid domainId, CheckType? checkType = null, int limit = 50)
    {
        var query = _dbContext.ProbeRuns
            .Include(pr => pr.Domain)
            .Where(pr => pr.DomainId == domainId);

        if (checkType.HasValue)
        {
            query = query.Where(pr => pr.CheckType == checkType.Value.ToString());
        }

        var probeRuns = await query
            .OrderByDescending(pr => pr.CompletedAt)
            .Take(limit)
            .ToListAsync();

        return probeRuns.Select(pr => new ProbeRunDto
        {
            Id = pr.Id,
            DomainId = pr.DomainId,
            DomainName = pr.Domain.Name,
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
        }).ToList();
    }

    public async Task<List<ProbeRunDto>> GetAllProbeRunsAsync(int limit = 100)
    {
        var probeRuns = await _dbContext.ProbeRuns
            .Include(pr => pr.Domain)
            .OrderByDescending(pr => pr.CompletedAt)
            .Take(limit)
            .ToListAsync();

        return probeRuns.Select(pr => new ProbeRunDto
        {
            Id = pr.Id,
            DomainId = pr.DomainId,
            DomainName = pr.Domain.Name,
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
        }).ToList();
    }
}
