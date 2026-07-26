namespace BrandSignal.Application.Common.Interfaces;

public interface ICampaignNotificationService
{
    Task NotificationAuditCompletedAsync(Guid campaignId, string status, string companyName);
    Task NotificationFailedAsync(Guid campaignId, string reason);
}