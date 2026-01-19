using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ObservabilityDns.Contracts.Enums;
using ObservabilityDns.Domain.DbContext;
using System.Net.Http.Json;
using System.Text.Json;

namespace ObservabilityDns.Worker.Services;

public class NotificationProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationProcessor> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationProcessor(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notifications");
            }

            // Wait 10 seconds before next check
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ObservabilityDnsDbContext>();

        var pendingNotifications = await dbContext.Notifications
            .Where(n => n.Status == "PENDING" && n.RetryCount < 3)
            .OrderBy(n => n.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var notification in pendingNotifications)
        {
            try
            {
                var success = await SendNotificationAsync(notification, cancellationToken);

                if (success)
                {
                    notification.Status = "SENT";
                    notification.ProcessedAt = DateTime.UtcNow;
                }
                else
                {
                    notification.RetryCount++;
                    if (notification.RetryCount >= 3)
                    {
                        notification.Status = "FAILED";
                    }

                    // Record attempt
                    var attempt = new ObservabilityDns.Domain.Entities.NotificationAttempt
                    {
                        NotificationId = notification.Id,
                        AttemptNumber = notification.RetryCount,
                        ErrorMessage = "Failed to send notification",
                        AttemptedAt = DateTime.UtcNow
                    };
                    dbContext.NotificationAttempts.Add(attempt);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification {NotificationId}", notification.Id);
                notification.RetryCount++;
                if (notification.RetryCount >= 3)
                {
                    notification.Status = "FAILED";
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task<bool> SendNotificationAsync(
        ObservabilityDns.Domain.Entities.Notification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            if (notification.Channel == "SLACK")
            {
                return await SendSlackNotificationAsync(notification, cancellationToken);
            }
            else if (notification.Channel == "EMAIL")
            {
                return await SendEmailNotificationAsync(notification, cancellationToken);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {Channel} notification", notification.Channel);
            return false;
        }
    }

    private async Task<bool> SendSlackNotificationAsync(
        ObservabilityDns.Domain.Entities.Notification notification,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        var payload = notification.Payload.RootElement;
        var slackPayload = new
        {
            text = payload.GetProperty("message").GetString() ?? "Domain monitoring alert",
            attachments = new[]
            {
                new
                {
                    color = "danger",
                    fields = new[]
                    {
                        new { title = "Domain", value = payload.GetProperty("domainName").GetString(), @short = true },
                        new { title = "Check Type", value = payload.GetProperty("checkType").GetString(), @short = true },
                        new { title = "Error", value = payload.GetProperty("error").GetString(), @short = false }
                    }
                }
            }
        };

        var response = await httpClient.PostAsJsonAsync(notification.Destination, slackPayload, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> SendEmailNotificationAsync(
        ObservabilityDns.Domain.Entities.Notification notification,
        CancellationToken cancellationToken)
    {
        // TODO: Implement SMTP email sending
        // For now, just log it
        _logger.LogInformation("Email notification would be sent to {Destination}", notification.Destination);
        return true; // Simulate success for now
    }
}
