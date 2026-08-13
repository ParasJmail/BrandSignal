using MediatR;

namespace BrandSignal.Application.Campaigns.Commands.CreateCampaign;

public record CreateCampaignCommand(string CompanyName, string TargetKeyword) : IRequest<Guid>;