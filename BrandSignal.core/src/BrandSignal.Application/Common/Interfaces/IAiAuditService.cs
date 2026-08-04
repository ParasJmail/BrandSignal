using BrandSignal.Application.Common.Models;

namespace BrandSignal.Application.Common.Interfaces;

public interface IAiAuditService
{
    Task<CampaignAuditResult> AnalyzeCampaignAsync(string companyName, string targetKeyword, CancellationToken cancellationToken = default);
}