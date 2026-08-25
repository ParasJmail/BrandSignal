namespace BrandSignal.Infrastructure.BackgroundJobs;

public class ScraperResultMessage
{
    public Guid CampaignId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TargetKeyword { get; set; } = string.Empty;
    public int SentimentScore { get; set; }
    public string BrandPositioning { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> TopCompetitors { get; set; } = new();
    public List<string> RecommendedKeywords { get; set; } = new();
    public List<string> ScrapedCitations { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}