using BrandSignal.Application;
using BrandSignal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register custom Application and Infrastructure layer services
builder.Services.AddApplicationServices();
builder.Services.AddInfraStructureServices(builder.Configuration);

builder.Services.AddControllers();

// Add Swagger/OpenAPI generation services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BrandSignal API V1");
        c.RoutePrefix = "swagger"; // Available at http://localhost:<port>/swagger
    });
}

app.UseAuthorization();

// Route incoming requests to our Controllers (like CampaignsController.cs)
app.MapControllers();

// Kick off the web server (MUST be at the very bottom of executable statements)
app.Run();