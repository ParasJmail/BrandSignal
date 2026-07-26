using BrandSignal.Application;
using BrandSignal.Infrastructure;
using BrandSignal.API.Middleware;
using BrandSignal.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. Add SignalR Services
builder.Services.AddSignalR();

// Register custom Application and Infrastructure layer services
builder.Services.AddApplicationServices();
builder.Services.AddInfraStructureServices(builder.Configuration);

builder.Services.AddControllers();

// Add Swagger/OpenAPI generation services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Adjust this to your frontend's URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); //REQUIRED for SignalR WebSockets
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Enable Exception Handling Middleware early in the pipeline
app.UseExceptionHandler();

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

app.UseHttpsRedirection();
app.UseAuthorization();

// Route incoming requests to our Controllers (like CampaignsController.cs)
app.MapControllers();

// 2. Map the SignalR Hub endpoint
app.MapHub<CampaignHub>("/hubs/campaigns");

// Kick off the web server (MUST be at the very bottom of executable statements)
app.Run();