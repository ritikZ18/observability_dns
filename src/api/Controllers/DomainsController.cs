using Microsoft.AspNetCore.Mvc;
using ObservabilityDns.Api.Services;
using ObservabilityDns.Contracts.DTOs;

namespace ObservabilityDns.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DomainsController : ControllerBase
{
    private readonly DomainService _domainService;
    private readonly ILogger<DomainsController> _logger;

    public DomainsController(DomainService domainService, ILogger<DomainsController> logger)
    {
        _domainService = domainService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<DomainDto>>> GetAllDomains([FromQuery] Guid? groupId = null)
    {
        var domains = await _domainService.GetAllDomainsAsync(groupId);
        return Ok(domains);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DomainDetailDto>> GetDomain(Guid id)
    {
        var domain = await _domainService.GetDomainByIdAsync(id);
        if (domain == null)
            return NotFound();

        return Ok(domain);
    }

    [HttpPost]
    public async Task<ActionResult<DomainDto>> CreateDomain([FromBody] CreateDomainRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var domain = await _domainService.CreateDomainAsync(request);
            return CreatedAtAction(nameof(GetDomain), new { id = domain.Id }, domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating domain {DomainName}", request.Name);
            return StatusCode(500, new { error = "Failed to create domain", message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDomain(Guid id, [FromBody] UpdateDomainRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _domainService.UpdateDomainAsync(id, request);
        if (!updated)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDomain(Guid id)
    {
        var deleted = await _domainService.DeleteDomainAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
