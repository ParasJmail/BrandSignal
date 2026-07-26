namespace BrandSignal.Application.Common.Interfaces;

public interface IRabbitMqService{
    // This method takes the unique Case Number (campaignId), 
    // the Company Name, and the Keyword, and prepares to ship it out to RabbitMQ.
    Task RequestAiAuditAsync(Guid campaignId, string companyName, string keyword);
}