using BrandSignal.Application.Campaigns.Commands.CreateCampaign;
using Microsoft.AspNetCore.Mvc;
using BrandSignal.Application.Campaigns.Queries.GetCampaignById;
using BrandSignal.Application.Campaigns.Queries.GetCampaigns;

namespace BrandSignal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]// Sets the base web address path to: api/campaigns
public class CampaignsController : ControllerBase
{
    private readonly CreateCampaignCommandHandler _commandHandler;
    private readonly GetCampaignByIdQueryHandler _getByIdHandler;
    private readonly GetCampaignsQueryHandler _getCampaignsHandler;

        // The front-desk constructor asks the runtime engine to hand it our Application Manager tool
    public CampaignsController(CreateCampaignCommandHandler commandHandler, GetCampaignByIdQueryHandler getByIdHandler, GetCampaignsQueryHandler getCampaignsHandler)
    {
        _commandHandler = commandHandler;
        _getByIdHandler = getByIdHandler;
        _getCampaignsHandler = getCampaignsHandler;
    }

    [HttpPost] // Marks this method to exclusively handle incoming HTTP POST web requests
    public async Task<IActionResult> CreateCampaign ([FromBody] CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        // 1. Hand the incoming web input note straight to our Application Manager engine
        // Pass the request's cancellationToken down to the handler
        Guid campaignId = await _commandHandler.HandleAsync(command, cancellationToken);

        // 2. Return an HTTP 200 OK status code along with the unique tracking barcode ID
        return Ok(new {id = campaignId});
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByID(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCampaignByIdQuery(id);
        var campaign = await _getByIdHandler.HandleAsync(query, cancellationToken);

        if (campaign is null)
        {
            return NotFound(new {Message = $"Campaign with ID {id} not found."});
        }

        return Ok(campaign);
    }

    [HttpGet]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] string? searchTerm,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetCampaignQuery(searchTerm, status, pageNumber, pageSize);
        var result = await _getCampaignsHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }
}