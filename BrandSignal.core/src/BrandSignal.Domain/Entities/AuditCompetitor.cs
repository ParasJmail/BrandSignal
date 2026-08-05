namespace BrandSignal.Domain.Entities;

public class AuditCompetitor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuditReportId { get; set; } // Foreign key to the AuditReport

    public string Name { get; set; } = string.Empty;

    // Navigation property back to AuditReport
    public AuditReport AuditReport { get; set; } = null!;
}