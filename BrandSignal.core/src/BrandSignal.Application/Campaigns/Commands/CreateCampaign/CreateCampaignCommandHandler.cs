using BrandSignal.Application.Common.Interaces;
using BrandSignal.Domain.Entities;

namespace BrandSignal.Application.Campaigns.Commands.CreateCampaign;

public class CreateCampaignCommandHandler{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRabbitMqService _rabbitMqService;

    // The manager constructor requests our database and messaging interface contracts
    public CreateCampaignCommandHandler(IApplicationDbContext dbContext, IRabbitMqService rabbitMqService){
        _dbContext = dbContext;
        _rabbitMqService = rabbitMqService;
    }

    // The core execution handler that processes the audit workflow step-by-step
    public async Task<Guid> HandleAsync(CreateCampaignCommand command, CancellationToken cancellationToken){
        // 1. Guard check: Ensure the user didn't submit blank strings
        if (string.IsNullOrWhiteSpace(command.CompanyName) || string.IsNullOrWhiteSpace(command.TargetKeyword))
        {
            throw new ArgumentException("Company Name and Target Keyword cannot be empty.");
        }

        // 2. Open a new Domain Case Folder (Campaign Entity) and write down the inputs
        var campaign = new Campaign
        {
            CompanyName = command.CompanyName,
            TargetKeyword = command.TargetKeyword,
            Status = "Processing" // Flip the tracking flag to processing instantly
        };

        // 3. Hand the folder to our database interface to stage and commit to SQL permanent storage
        _dbContext.Campaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Alert our messaging interface to dispatch a network ticket to the Node.js scraper
        await _rabbitMqService.RequestAiAuditAsync(campaign.Id, campaign.CompanyName, campaign.TargetKeyword);

        // 5. Hand the unique tracking barcode back to the caller
        return campaign.Id;
    }
}