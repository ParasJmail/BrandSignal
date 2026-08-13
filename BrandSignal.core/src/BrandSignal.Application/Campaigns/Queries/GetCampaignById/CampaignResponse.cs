namespace BrandSignal.Application.Campaigns.Queries.GetCampaignById;

public record CampaignResponse(
    Guid Id,
    string CompanyName,
    string TargetKeyword,
    string Status,
    int VisibilityScore,
    string AuditSummary,
    DateTime CreatedAtUtc,
    DateTime? AuditedAtUtc,
    List<AuditReportResponse> AuditReports
);