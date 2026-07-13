using System.Text;
using System.Text.Json;
using BrandSignal.Application.Common.Interaces;
using RabbitMQ.Client;

namespace BrandSignal.Infrastructure.Messaging;

// This class signs our interface contract, promising to connect our app to the message lines.
public class RabbitMQService :IRabbitMqService{
    // This fulfills our contract's promise to provide an AI worker dispatch button.
    public async RequestAiAuditAsync(Guid campaignId, string companyName, string keyword){
        // 1. Create a lightweight anonymous data packet carrying our audit parameters
        var messagePayload = new{
            CampaignId = campaignId,
            CompanyName = companyName,
            TargetKeyword = keyword,
            TimeStamp = DateTime.UtcNow
        }

        // 2. Convert the C# data packet into a plain text string (JSON)
        const jsonString = JsonSerializer.Serialize(messagePayload);

        // 3. Convert that text string into raw binary bytes so it can travel over network wires
        byte[] body = Encoding.UTF8.GetBytes(jsonString);

        // 4. Setup our connection factory to find the local RabbitMQ server pipeline
        var factory = new ConnectionFactory {HostName = "localhost"};

        // 5. Open the physical network socket connection and create an execution channel
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // 6. Declare a secure queue message line named "campaign_audit_queue" 
        // ensuring it won't crash even if the server restarts (durable: true)
        await channel.QueueDeclareAsync(
            queue : "campaign_audit_queue",
            durable : true,
            exclusive : false,
            autoDelete : false,
            arguments : null
        );

        // 7. Publish the binary data packet straight into the queue line
        await channel.BasicPublishAsync(
            exchange : string.Empty,
            routingKey : "campaign_audit_queue",
            mandatory : true,
            body : body
        );

    }
}