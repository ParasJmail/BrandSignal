using System.Text;
using System.Text.Json;
using BrandSignal.Application.Common.Interfaces;
using BrandSignal.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BrandSignal.Infrastructure.BackgroundJobs;

public class CampaignAuditWorker : BackgroundService
{
    private readonly ILogger<CampaignAuditWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ICampaignNotificationService _notificationService;

    // Node.js Scraper publishes completed results onto this queue
    private const string ResultsQueue = "audit_results_queue";
    private const string DlxExchange = "campaign_audit_dlx";
    private const string DlqQueue = "campaign_audit_dlq";
    private const string DlqRoutingKey = "campaign.audit.deadletter";

    private readonly AsyncRetryPolicy _retryPolicy;

    public CampaignAuditWorker(
        ILogger<CampaignAuditWorker> logger,
        IServiceScopeFactory scopeFactory,
        ICampaignNotificationService notificationService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;

        // Configure Polly Async Retry Policy: 3 retries with exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Transient error occurred while saving audit results to DB. Retrying in {TimeSpan} seconds. Retry attempt {RetryCount}.",
                        timeSpan.TotalSeconds, retryCount);
                }
            );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // 1. Declare Dead Letter Exchange (DLX) & Queue (DLQ)
            await _channel.ExchangeDeclareAsync(
                exchange: DlxExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken
            );

            await _channel.QueueDeclareAsync(
                queue: DlqQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken
            );

            await _channel.QueueBindAsync(
                queue: DlqQueue,
                exchange: DlxExchange,
                routingKey: DlqRoutingKey,
                cancellationToken: stoppingToken);

            // 2. Declare the Results Queue (consumed from Node.js scraper)
            await _channel.QueueDeclareAsync(
                queue: ResultsQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            _logger.LogInformation("CampaignAuditWorker connected. Listening for scraper results on queue: '{ResultsQueue}'", ResultsQueue);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received audit result payload from queue: {Message}", message);

                ScraperResultMessage? auditResult = null;

                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    auditResult = JsonSerializer.Deserialize<ScraperResultMessage>(message, options);

                    if (auditResult != null && auditResult.CampaignId != Guid.Empty)
                    {
                        // Save to SQL Database with Polly retry policy
                        await _retryPolicy.ExecuteAsync(async () =>
                        {
                            await SaveAuditResultToDatabaseAsync(auditResult, stoppingToken);
                        });
                    }

                    // Acknowledge message delivery
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Permanent failure saving audit result for message: {Message}. Routing to DLQ.", message);

                    if (auditResult != null && auditResult.CampaignId != Guid.Empty)
                    {
                        await MarkCampaignAsFailedAsync(auditResult.CampaignId, ex.Message, stoppingToken);
                    }

                    // Requeue = false sends poison message to DLQ
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(queue: ResultsQueue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to RabbitMQ queue: {ResultsQueue}", ResultsQueue);
        }
    }

    private async Task SaveAuditResultToDatabaseAsync(ScraperResultMessage result, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var campaign = await context.Campaigns.FindAsync(new object[] { result.CampaignId }, cancellationToken);

        if (campaign is null)
        {
            _logger.LogWarning("Campaign with ID {CampaignId} not found in database.", result.CampaignId);
            return;
        }

        // 1. Build relational AuditReport from Scraper result
        var auditReport = new AuditReport
        {
            CampaignId = campaign.Id,
            SentimentScore = result.SentimentScore,
            BrandPositioning = result.BrandPositioning,
            Summary = result.Summary,
            CreatedAt = DateTime.UtcNow,
            Competitors = result.TopCompetitors
                .Select(name => new AuditCompetitor { Name = name })
                .ToList(),
            RecommendedKeywords = result.RecommendedKeywords
                .Select(keyword => new AuditRecommendedKeyword { Keyword = keyword })
                .ToList()
        };

        // 2. Transition campaign status and persist score/summary
        campaign.Status = "Completed";
        campaign.VisibilityScore = result.SentimentScore;
        campaign.AuditSummary = result.Summary;
        campaign.AuditedAt = DateTime.UtcNow;

        context.AuditReports.Add(auditReport);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully saved audit report from Scraper for Campaign: {CompanyName} ({CampaignId})", campaign.CompanyName, campaign.Id);

        // 3. Notify connected web clients via SignalR
        await _notificationService.NotificationAuditCompletedAsync(campaign.Id, "Completed", campaign.CompanyName);
    }

    public async Task MarkCampaignAsFailedAsync(Guid campaignId, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var campaign = await context.Campaigns.FindAsync(new object[] { campaignId }, cancellationToken);

            if (campaign is not null)
            {
                campaign.Status = "Failed";
                campaign.AuditSummary = $"Audit Failed: {errorMessage}";
                campaign.AuditedAt = DateTime.UtcNow;

                await context.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("Updated Campaign ID {CampaignId} status to 'Failed' in database.", campaignId);

                await _notificationService.NotificationAuditCompletedAsync(campaignId, "Failed", campaign.CompanyName);
            }
            else
            {
                _logger.LogWarning("Campaign with ID {CampaignId} not found in database. Cannot mark as failed.", campaignId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Campaign ID {CampaignId} status to 'Failed' in database.", campaignId);
        }
    }

    public override void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        base.Dispose();
    }
}