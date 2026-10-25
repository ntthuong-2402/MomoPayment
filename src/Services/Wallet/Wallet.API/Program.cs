using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MomoPayment.Caching;
using MomoPayment.Common.Middlewares;
using MomoPayment.Common.Security;
using MomoPayment.EventBus;
using Wallet.API.Consumers;
using Wallet.API.Data;
using Wallet.API.GrpcServices;
using Wallet.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Database
builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Redis & Distributed Locks
builder.Services.AddMomoRedis(builder.Configuration);

// Add EventBus (RabbitMQ)
builder.Services.AddMomoEventBus(builder.Configuration);
builder.Services.AddTransient<UserRegisteredConsumer>();
builder.Services.AddHostedService<WalletEventBusSubscriptionHostedService>();

// Add JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

builder.Services.AddAuthorization();

// Add gRPC
builder.Services.AddGrpc();

// Add Application Services
builder.Services.AddScoped<IWalletService, WalletService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MoMo Wallet Microservice API",
        Version = "v1",
        Description = "Microservice quản lý số dư ví, nạp/rút tiền, gRPC internal service"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

var app = builder.Build();

app.UseCors("AllowAll");

// Ensure Database is created and seeded with retry policy for container cold-starts
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    const int maxRetries = 10;
    for (int retry = 1; retry <= maxRetries; retry++)
    {
        try
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Wallet database created and verified successfully.");
            break;
        }
        catch (Exception ex)
        {
            if (retry == maxRetries)
            {
                logger.LogError(ex, "Could not automatically initialize database after {MaxRetries} attempts.", maxRetries);
                throw;
            }
            else
            {
                logger.LogWarning("Waiting for SQL Server to be ready (attempt {Attempt}/{MaxRetries}): {Message}", retry, maxRetries, ex.Message);
                Thread.Sleep(3000);
            }
        }
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wallet API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

// Map gRPC service
app.MapGrpcService<WalletGrpcServiceImpl>();

// Map REST Controllers
app.MapControllers();

app.Run();
