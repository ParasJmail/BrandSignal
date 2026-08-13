using BrandSignal.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using BrandSignal.Application.Campaigns.Queries.GetCampaigns;
using MediatR;

namespace BrandSignal.Application.Campaigns.Queries.GetCampaignById;

public class GetCampaignByIdQueryHandler : IRequestHandler<GetCampaignByIdQuery, CampaignResponse>
{
    public readonly IApplicationDbContext _context;

    public GetCampaignByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CampaignResponse> Handle(GetCampaignByIdQuery query, CancellationToken cancellationToken)
    {
        var campaign = await _context.Campaigns
            .AsNoTracking()
            .Include(c => c.AuditReports)
                .ThenInclude(r => r.Competitors)
            .Include(c => c.AuditReports)
                .ThenInclude(r => r.RecommendedKeywords)
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

        if (campaign is null)
        {
            throw new KeyNotFoundException($"Campaign with ID {query.Id} not found.");
        }

        return new CampaignResponse(
            campaign.Id,
            campaign.CompanyName,
            campaign.TargetKeyword,
            campaign.Status.ToString(),
            campaign.VisibilityScore,
            campaign.AuditSummary,
            campaign.CreatedAt,
            campaign.AuditedAt,
            campaign.AuditReports
                .OrderByDescending(r => r.CreatedAt)
                .Select(report => new AuditReportResponse(
                    report.Id,
                    report.SentimentScore,
                    report.BrandPositioning,
                    report.Summary,
                    report.CreatedAt,
                    report.Competitors.Select(c => c.Name).ToList(),
                    report.RecommendedKeywords.Select(k => k.Keyword).ToList()
                )).ToList()
        );
    }
}