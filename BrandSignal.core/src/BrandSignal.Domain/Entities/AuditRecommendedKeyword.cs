namespace BrandSignal.Domain.Entities;

public class AuditRecommendedKeyword
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuditReportId { get; set; } // Foreign key to the AuditReport
    public string Keyword { get; set; } = string.Empty;
    public AuditReport AuditReport { get; set; } = null!; // Navigation property back to AuditReport
}