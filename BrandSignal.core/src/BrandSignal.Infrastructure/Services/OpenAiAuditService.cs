using System.Net.Http.Json;
using System.Text.Json;
using BrandSignal.Application.Common.Interfaces;
using BrandSignal.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrandSignal.Infrastructure.Services;

public class OpenAiAuditService: IAiAuditService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiAuditService> _logger;

    public OpenAiAuditService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiAuditService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CampaignAuditResult> AnalyzeCampaignAsync(string companyName, string targetKeyword, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenAi:ApiKey"];

        // Fallback for local dev if API key is not yet configured
        if(string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_OPENAI_API_KEY")
        {
            _logger.LogWarning("OpenAI API key is not configured. Returning fallback structured audit data.");
            return GenerateFallbackReport(companyName, targetKeyword);
        }

        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are an expert AI brand strategist. Analyze the given company and target keyword, and respond ONLY with a valid JSON object matching the requested schema."
                },
                new
                {
                    role = "user",
                    content = $"Analyze Company: '{companyName}' targeting Keyword: '{targetKeyword}'."
                }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "campaign_audit",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            sentimentScore = new { type = "integer", description = "Score between 0 and 100" },
                            brandPositioning = new { type = "string" },
                            topCompetitors = new { type = "array", items = new { type = "string" } },
                            recommendedKeywords = new { type = "array", items = new { type = "string" } },
                            summary = new { type = "string" }
                        },
                        required = new[] { "sentimentScore", "brandPositioning", "topCompetitors", "recommendedKeywords", "summary" },
                        additionalProperties = false
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsonDoc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        var jsonContent = jsonDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        var result = JsonSerializer.Deserialize<CampaignAuditResult>(
            jsonContent!, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return result ?? GenerateFallbackReport(companyName, targetKeyword);
    }

    private static CampaignAuditResult GenerateFallbackReport(string companyName, string targetKeyword)
    {
        return new CampaignAuditResult(
            SentimentScore: 85,
            BrandPositioning: $"{companyName} holds a strong market presence around '{targetKeyword}'.",
            TopCompetitors: new List<string> { "Competitor A", "Competitor B", "Competitor C" },
            RecommendedKeywords: new List<string> { $"{targetKeyword} solutions", $"best {targetKeyword}", $"{companyName} vs industry" },
            Summary: $"Automated fallback audit completed for {companyName} targeting {targetKeyword}."
        );
    }
}