using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ObservabilityDns.Api.Controllers;

/// <summary>
/// Browser-to-API measurements. These endpoints are deliberately independent of
/// the DNS/TLS/HTTP probe workers: the visitor's browser initiates all traffic.
/// </summary>
[ApiController]
[Route("api/speed-test")]
[EnableRateLimiting("speed-test")]
public sealed class SpeedTestController : ControllerBase
{
    // Limit bandwidth/cost per request. Public deployments should also use
    // provider-level WAF/quotas and dedicated measurement infrastructure.
    private const int MaxDownloadBytes = 16 * 1024 * 1024;
    private const long MaxUploadBytes = 4 * 1024 * 1024;

    private void SetMeasurementHeaders()
    {
        Response.Headers["Cache-Control"] = "no-store, no-transform";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Timing-Allow-Origin"] = "*";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        SetMeasurementHeaders();
        return NoContent();
    }

    [HttpGet("download")]
    public async Task Download([FromQuery] int bytes, CancellationToken cancellationToken)
    {
        SetMeasurementHeaders();

        if (bytes < 1 || bytes > MaxDownloadBytes)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Invalid download size.", cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/octet-stream";
        Response.ContentLength = bytes;

        var buffer = new byte[64 * 1024];
        var remaining = bytes;
        while (remaining > 0)
        {
            var count = Math.Min(remaining, buffer.Length);
            // Avoid very compressible zero-filled payloads.
            RandomNumberGenerator.Fill(buffer.AsSpan(0, count));
            await Response.Body.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            remaining -= count;
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        SetMeasurementHeaders();

        if (Request.ContentLength is > MaxUploadBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var total = 0L;
        var buffer = new byte[64 * 1024];
        int count;

        while ((count = await Request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += count;
            if (total > MaxUploadBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            }
        }

        return Ok(new { bytesReceived = total });
    }
}
