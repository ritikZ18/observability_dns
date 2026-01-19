using Microsoft.AspNetCore.Mvc;
using ObservabilityDns.Api.Services;
using ObservabilityDns.Contracts.DTOs;

namespace ObservabilityDns.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebsiteInfoController : ControllerBase
{
    private readonly WebsiteInfoService _websiteInfoService;
    private readonly ILogger<WebsiteInfoController> _logger;

    public WebsiteInfoController(WebsiteInfoService websiteInfoService, ILogger<WebsiteInfoController> logger)
    {
        _websiteInfoService = websiteInfoService;
        _logger = logger;
    }

    [HttpGet("{domain}")]
    public async Task<ActionResult<WebsiteInfoDto>> GetWebsiteInfo(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return BadRequest(new { error = "Domain is required" });

        try
        {
            var info = await _websiteInfoService.GetWebsiteInfoAsync(domain);
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching website info for {Domain}", domain);
            return StatusCode(500, new { error = "Failed to fetch website info", message = ex.Message });
        }
    }
}
