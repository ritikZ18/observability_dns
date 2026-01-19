using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObservabilityDns.Contracts.DTOs;
using ObservabilityDns.Contracts.Enums;
using ObservabilityDns.Domain.DbContext;

namespace ObservabilityDns.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidentsController : ControllerBase
{
    private readonly ObservabilityDnsDbContext _dbContext;
    private readonly ILogger<IncidentsController> _logger;

    public IncidentsController(ObservabilityDnsDbContext dbContext, ILogger<IncidentsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<IncidentDto>>> GetIncidents(
        [FromQuery] IncidentStatus? status = null,
        [FromQuery] Guid? domainId = null)
    {
        var query = _dbContext.Incidents
            .Include(i => i.Domain)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value.ToString());
        }

        if (domainId.HasValue)
        {
            query = query.Where(i => i.DomainId == domainId.Value);
        }

        var incidents = await query
            .OrderByDescending(i => i.StartedAt)
            .ToListAsync();

        var result = incidents.Select(i => new IncidentDto
        {
            Id = i.Id,
            DomainId = i.DomainId,
            DomainName = i.Domain.Name,
            CheckType = Enum.Parse<CheckType>(i.CheckType),
            Severity = Enum.Parse<Severity>(i.Severity),
            Status = Enum.Parse<IncidentStatus>(i.Status),
            Reason = i.Reason,
            StartedAt = i.StartedAt,
            ResolvedAt = i.ResolvedAt,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IncidentDto>> GetIncident(Guid id)
    {
        var incident = await _dbContext.Incidents
            .Include(i => i.Domain)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident == null)
            return NotFound();

        var result = new IncidentDto
        {
            Id = incident.Id,
            DomainId = incident.DomainId,
            DomainName = incident.Domain.Name,
            CheckType = Enum.Parse<CheckType>(incident.CheckType),
            Severity = Enum.Parse<Severity>(incident.Severity),
            Status = Enum.Parse<IncidentStatus>(incident.Status),
            Reason = incident.Reason,
            StartedAt = incident.StartedAt,
            ResolvedAt = incident.ResolvedAt,
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt
        };

        return Ok(result);
    }

    [HttpPut("{id}/resolve")]
    public async Task<IActionResult> ResolveIncident(Guid id)
    {
        var incident = await _dbContext.Incidents.FindAsync(id);
        if (incident == null)
            return NotFound();

        incident.Status = IncidentStatus.RESOLVED.ToString();
        incident.ResolvedAt = DateTime.UtcNow;
        incident.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}
