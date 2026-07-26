using BrandSignal.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using BrandSignal.Infrastructure.Hubs;

namespace BrandSignal.Infrastructure.Services;

public class SignalRNotificationService : ICampaignNotificationService
{
    private readonly IHubContext<CampaignHub> _hubContext;

    // Inject untyped IHubContext provided by Microsoft.AspNetCore.SignalR.Core
    public SignalRNotificationService(IHubContext<CampaignHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotificationAuditCompletedAsync(Guid campaignId, string status, string companyName)
    {
        await _hubContext.Clients
            .All
            // .Group(campaignId.ToString())
            .SendAsync("CampaignAuditCompleted", campaignId, status, companyName);
    }

    public async Task NotificationFailedAsync(Guid campaignId, string reason)
    {
        await _hubContext.Clients
            .All
            // .Group(campaignId.ToString())
            .SendAsync("CampaignAuditFailed", campaignId, reason);
    }
}