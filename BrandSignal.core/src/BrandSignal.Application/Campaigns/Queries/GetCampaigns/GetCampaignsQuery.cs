namespace BrandSignal.Application.Campaigns.Queries.GetCampaigns;

public record GetCampaignQuery(
    string? SearchTerm = null,
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 10
);