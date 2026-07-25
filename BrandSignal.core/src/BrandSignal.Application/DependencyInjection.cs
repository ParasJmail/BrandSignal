using BrandSignal.Application.Campaigns.Commands.CreateCampaign;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

namespace BrandSignal.Application;

public static class DependencyInjection
{
    // This adds a registry button to catalog all our pure core manager services
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register our specific creation manager so the API controllers can find it
        services.AddScoped<CreateCampaignCommandHandler>();
        // Automatically registers all validators in the Application assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}