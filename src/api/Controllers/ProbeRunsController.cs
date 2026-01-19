using Microsoft.AspNetCore.Mvc;
using ObservabilityDns.Api.Services;
using ObservabilityDns.Contracts.DTOs;
using ObservabilityDns.Contracts.Enums;

namespace ObservabilityDns.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProbeRunsController : ControllerBase
{
    private readonly ProbeRunService _probeRunService;
    private readonly ILogger<ProbeRunsController> _logger;

    public ProbeRunsController(ProbeRunService probeRunService, ILogger<ProbeRunsController> logger)
    {
        _probeRunService = probeRunService;
        _logger = logger;
    }

    [HttpGet("domains/{domainId}")]
    public async Task<ActionResult<List<ProbeRunDto>>> GetProbeRunsByDomain(
        Guid domainId,
        [FromQuery] CheckType? checkType = null,
        [FromQuery] int limit = 50)
    {
        var probeRuns = await _probeRunService.GetProbeRunsByDomainIdAsync(domainId, checkType, limit);
        return Ok(probeRuns);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProbeRunDto>>> GetAllProbeRuns([FromQuery] int limit = 100)
    {
        var probeRuns = await _probeRunService.GetAllProbeRunsAsync(limit);
        return Ok(probeRuns);
    }
}
