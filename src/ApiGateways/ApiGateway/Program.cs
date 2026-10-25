using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MoMo Payment Microservices API Gateway",
        Version = "v1",
        Description = "Central API Gateway for MoMo Payment ecosystem"
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        c.SwaggerEndpoint("http://localhost:5001/swagger/v1/swagger.json", "Identity Service");
        c.SwaggerEndpoint("http://localhost:5002/swagger/v1/swagger.json", "Wallet Service");
        c.SwaggerEndpoint("http://localhost:5003/swagger/v1/swagger.json", "Transaction Service");
        c.SwaggerEndpoint("http://localhost:5004/swagger/v1/swagger.json", "Notification Service");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", service = "ApiGateway", timestamp = DateTime.UtcNow }));

// Map Reverse Proxy routes
app.MapReverseProxy();

app.Run();

