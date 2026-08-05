namespace BrandSignal.Domain.Entities;

public class AuditReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; } // Foreign key to the Campaign

    public int SentimentScore { get; set; }
    public string BrandPositioning { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relational Collection
    public List<AuditCompetitor> Competitors { get; set;} = new();
    public List<AuditRecommendedKeyword> RecommendedKeywords { get; set;} = new();

    // Navigation property back to Campaign
    public Campaign Campaign { get; set; } = null!;
}