namespace BrandSignal.Application.Campaigns.Queries.GetCampaignById;

public record CampaignResponse(
    Guid Id,
    string CompanyName,
    string TargetKeyword,
    string Status,
    DateTime CreatedAtUtc
);