using BrandSignal.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using BrandSignal.Application.Campaigns.Queries.GetCampaigns;

namespace BrandSignal.Application.Campaigns.Queries.GetCampaignById;

public class GetCampaignByIdQueryHandler
{
    public readonly IApplicationDbContext _context;

    public GetCampaignByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CampaignResponse> HandleAsync(GetCampaignByIdQuery query, CancellationToken cancellationToken)
    {
        var campaign = await _context.Campaigns
            .AsNoTracking() // Read-only performance boost: we don't need to track changes for this query
            .FirstOrDefaultAsync( c => c.Id == query.Id , cancellationToken);

        if (campaign is null)
        {
            throw new KeyNotFoundException($"Campaign with ID {query.Id} not found.");
        }

        return new CampaignResponse(
            campaign.Id,
            campaign.CompanyName,
            campaign.TargetKeyword,
            campaign.Status.ToString(),
            campaign.CreatedAt
        );
    }
}