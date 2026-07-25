using BrandSignal.Application.Campaigns.Commands.CreateCampaign;
using Microsoft.AspNetCore.Mvc;

namespace BrandSignal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]// Sets the base web address path to: api/campaigns
public class CampaignsController : ControllerBase
{
    private readonly CreateCampaignCommandHandler _commandHandler;

        // The front-desk constructor asks the runtime engine to hand it our Application Manager tool
    public CampaignsController(CreateCampaignCommandHandler commandHandler)
    {
        _commandHandler = commandHandler;
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
}