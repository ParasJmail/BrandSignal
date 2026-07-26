using BrandSignal.Application.Common.Interfaces;
using BrandSignal.Infrastructure.Messaging;
using BrandSignal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BrandSignal.Infrastructure;

public static class DependencyInjection{
    // This extension method acts as the registry clerk for our infrastructure tools
    public static IServiceCollection AddInfraStructureServices( this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Read our connection settings capsule and wire up our SQL Server Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // 2. Map our custom database interface contract to the real implementation class
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // 3. Map our message queue interface contract to the real RabbitMQ service client
        services.AddScoped<IRabbitMqService, RabbitMQService>();

        return services;
    }
}