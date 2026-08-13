using BrandSignal.Application.Campaigns.Queries.GetCampaignById;
using BrandSignal.Application.Common.Interfaces;
using BrandSignal.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrandSignal.Application.Campaigns.Queries.GetCampaigns;

public class GetCampaignsQueryHandler : IRequestHandler<GetCampaignQuery, PaginatedList<CampaignResponse>>
{
    public readonly IApplicationDbContext _context;

    public GetCampaignsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<CampaignResponse>> Handle(GetCampaignQuery query, CancellationToken cancellationToken)
    {
        // 1. Start with base IQueryable (No tracking for performance)
        var collection = _context.Campaigns.AsNoTracking();

        // 2. Apply Search Filter (Company Name or Target Keyword)
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            collection = collection.Where( c => 
                c.CompanyName.ToLower().Contains(term) ||
                c.TargetKeyword.ToLower().Contains(term));
        }

        // 3. Apply Status Filter
        if(!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLower();
            collection = collection.Where(c => c.Status.ToString().ToLower() == status);
        }

        // 4. Count total records matching filter criteria
        var totalCount = await collection.CountAsync(cancellationToken);

        // 5. Apply Pagination and Fetch Data
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize; // Cap max page size to 100

        var items = await collection
            .OrderByDescending( c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignResponse(
                c.Id,
                c.CompanyName,
                c.TargetKeyword,
                c.Status.ToString(),
                c.VisibilityScore,
                c.AuditSummary,
                c.CreatedAt,
                c.AuditedAt,
                new List<AuditReportResponse>()
            ))
            .ToListAsync(cancellationToken);
        
        return new PaginatedList<CampaignResponse>(items, totalCount, pageNumber, pageSize);
    }
}