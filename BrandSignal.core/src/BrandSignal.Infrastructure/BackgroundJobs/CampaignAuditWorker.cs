using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using BrandSignal.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using BrandSignal.Domain.Entities;
using Polly;
using Polly.Retry;

namespace BrandSignal.Infrastructure.BackgroundJobs;

public class CampaignAuditWorker : BackgroundService
{
    private readonly ILogger<CampaignAuditWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private ICampaignNotificationService _notificationService;

    private const string MainExchange = "campaign_audit_exchange";
    private const string MainQueue = "campaign_audit_queue";
    private const string MainRoutingKey = "campaign.audit.created";
    
    private const string DlxExchange = "campaign_audit_dlx";
    private const string DlqQueue = "campaign_audit_dlq";
    private const string DlqRoutingKey = "campaign.audit.deadletter";

    private readonly AsyncRetryPolicy _retryPolicy;

    public CampaignAuditWorker(ILogger<CampaignAuditWorker> logger, IServiceScopeFactory scopeFactory, ICampaignNotificationService notificationService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;

        // Configure Polly Async Retry Policy: # retries with exponential backoff (2s, 4s, 8s)
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Transient error occured while processing campaign audit. Retrying  in {TimeSpan} seconds. Retry attempt {RetryCount}.",
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
            _channel = await _connection.CreateChannelAsync(cancellationToken : stoppingToken);

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

            // 2. Declare Main Queue configured with DLX arguments
            var mainQueueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", DlxExchange },
                { "x-dead-letter-routing-key", DlqRoutingKey }
            };

            await _channel.QueueDeclareAsync(
                queue: MainQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: mainQueueArgs,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            _logger.LogInformation("Successfully connected to RabbitMQ queue. Main Queue: {MainQueue}, DLQ: {DlqQueue}", MainQueue, DlqQueue);

            // v7 uses Async EventingBasicConsumer
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received message from queue: {Message}", message);

                try
                {
                    await _retryPolicy.ExecuteAsync( async () =>
                    {
                        var auditMessage = JsonSerializer.Deserialize<CampaignCreatedMessage>(message);

                        if(auditMessage != null)
                        {
                            await ProcessCampaignAuditAsync(auditMessage.CampaignId, stoppingToken);
                        }
                    });

                    // Acknowledge successful processing
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {Message}", message);

                    // Requeue = false instructs RabbitMQ to forward this poison message to the DLX!
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(queue: MainQueue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to RabbitMQ queue: {MainQueue}", MainQueue);
        }
    }

    private async Task ProcessCampaignAuditAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // 1. Resolve IAiAuditService inside the scope to ensure it has the correct lifetime
        var aiAuditService = scope.ServiceProvider.GetRequiredService<IAiAuditService>();

        var campaign = await context.Campaigns.FindAsync(new object[] { campaignId }, cancellationToken);

        if(campaign is null)
        {
            _logger.LogWarning("Campaign with ID {CampaignId} not found.", campaignId);
            return;
        }

        _logger.LogInformation("Starting AI audit simulation for Campaign: {CompanyName}, Campaign ID: {CampaignId}", campaign.CompanyName, campaign.Id);

        // a. Execute AI audit call
        // 2. Call real AI audit service instead of Task.Delay
        var auditResult = await aiAuditService.AnalyzeCampaignAsync(campaign.CompanyName, campaign.TargetKeyword, cancellationToken);

        _logger.LogInformation("AI Audit complete. Sentiment Score: {Score}/100", auditResult.SentimentScore);

        // b. Map AI output ontp relational AuditReport entity and child collectiosn save to database
        var auditReport = new AuditReport
        {
            CampaignId = campaign.Id,
            SentimentScore = auditResult.SentimentScore,
            BrandPositioning = auditResult.BrandPositioning,
            Summary = auditResult.Summary,
            CreatedAt = DateTime.UtcNow,
            Competitors = auditResult.TopCompetitors
                .Select(name => new AuditCompetitor { Name = name })
                .ToList(),
            RecommendedKeywords = auditResult.RecommendedKeywords
                .Select(keyword => new AuditRecommendedKeyword { Keyword = keyword })
                .ToList()
        };

        // c. Update coreCampaign fields and save AuditReport entity to database
        // 3. Update database state
        campaign.Status = "Completed";
        campaign.VisibilityScore = auditResult.SentimentScore;
        campaign.AuditSummary = auditResult.Summary;
        campaign.AuditedAt = DateTime.UtcNow;

        // d. Save AuditReport entity to database
        context.AuditReports.Add(auditReport);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully completed AI audit for Campaign: {CompanyName}, Campaign ID: {CampaignId}", campaign.CompanyName, campaign.Id);

        // Notify connected web clients in real time via SignalR
        // 4. Notify clients via SignalR
        await _notificationService.NotificationAuditCompletedAsync(campaignId, "Completed", campaign.CompanyName);
    }

    public override void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        base.Dispose();
    }
}

public record CampaignCreatedMessage(Guid CampaignId);