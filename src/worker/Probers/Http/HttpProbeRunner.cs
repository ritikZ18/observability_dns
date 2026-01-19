using ObservabilityDns.Worker.Probers;
using System.Diagnostics;

namespace ObservabilityDns.Worker.Probers.Http;

public class HttpProbeRunner : IHttpProbeRunner
{
    private readonly ILogger<HttpProbeRunner> _logger;
    private readonly HttpClient _httpClient;

    public HttpProbeRunner(ILogger<HttpProbeRunner> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<ProbeResult> RunProbeAsync(string target, CancellationToken cancellationToken = default)
    {
        return await CheckUrlAsync(target, cancellationToken);
    }

    public async Task<HttpProbeResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var result = new HttpProbeResult();
        var stopwatch = Stopwatch.StartNew();

        // Normalize URL
        var normalizedUrl = NormalizeUrl(url);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, normalizedUrl);
            request.Headers.Add("User-Agent", "ObservabilityDNS/1.0");

            var responseStopwatch = Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request, cancellationToken);
            responseStopwatch.Stop();

            result.StatusCode = (int)response.StatusCode;
            result.TtfbMs = (int)responseStopwatch.ElapsedMilliseconds;

            // Capture headers
            foreach (var header in response.Headers)
            {
                result.Headers[header.Key] = string.Join(", ", header.Value);
            }

            result.Success = response.IsSuccessStatusCode;
            if (!result.Success)
            {
                result.ErrorCode = $"HTTP_{result.StatusCode}";
                result.ErrorMessage = $"HTTP {result.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            result.Success = false;
            result.ErrorCode = "TIMEOUT";
            result.ErrorMessage = "Request timed out";
            _logger.LogWarning("HTTP request timeout for {Url}", normalizedUrl);
        }
        catch (HttpRequestException ex)
        {
            result.Success = false;
            result.ErrorCode = "HTTP_ERROR";
            result.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "HTTP request failed for {Url}", normalizedUrl);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorCode = "UNKNOWN";
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error checking HTTP for {Url}", normalizedUrl);
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    private string NormalizeUrl(string input)
    {
        var url = input.Trim();
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }
        return url;
    }
}
