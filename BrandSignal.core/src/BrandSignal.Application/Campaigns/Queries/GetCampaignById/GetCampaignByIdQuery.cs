using BrandSignal.Application.Common.Models;
using MediatR;

namespace BrandSignal.Application.Campaigns.Queries.GetCampaignById;

public record GetCampaignByIdQuery(Guid Id) : IRequest<CampaignResponse>;