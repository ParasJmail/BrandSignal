using BrandSignal.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BrandSignal.Infrastructure.Hubs;

public class CampaignHub : Hub
{
    // Temporary letting the notification available to every client will set up company based notification system once we have the company context available in the SignalR connection
    // public async Task JoinCampaignGroup(string campaignId)
    // {
    //     await Groups.AddToGroupAsync(Context.ConnectionId, campaignId);
    // }

    // // Clients call this when navigating away from a campaign
    // public async Task LeaveCampaignGroup(string campaignId)
    // {
    //     await Groups.RemoveFromGroupAsync(Context.ConnectionId, campaignId);
    // }
}