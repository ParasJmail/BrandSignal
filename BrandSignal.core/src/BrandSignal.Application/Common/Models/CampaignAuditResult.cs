namespace BrandSignal.Application.Common.Models;

public record CampaignAuditResult
(
    int SentimentScore,
    string BrandPositioning,
    List<string> TopCompetitors,
    List<string> RecommendedKeywords,
    string Summary
);