namespace BrandSignal.Application.Campaigns.Queries.GetCampaignById;

public record AuditReportResponse(
    Guid Id,
    int SentimentScore,
    string BrandPositioning,
    string Summary,
    DateTime CreatedAtUtc,
    List<string> Competitors,
    List<string> RecommendedKeywords
);