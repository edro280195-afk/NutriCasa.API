using System.Security.Claims;
using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NutriCasa.Api.Middleware;
using NutriCasa.Application;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Infrastructure;
using NutriCasa.Infrastructure.BackgroundJobs;
using NutriCasa.Infrastructure.Persistence.Seeds;
using NutriCasa.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("logs/nutricasa-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Capas
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NutriCasa API", Version = "v1", Description = "API de coaching nutricional cetogénico para grupos familiares" });
    c.EnableAnnotations();
});
builder.Services.AddSignalR();

// File storage
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
builder.Services.AddScoped<IFileStorageService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config["Storage:Provider"];

    if (string.Equals(provider, "R2", StringComparison.OrdinalIgnoreCase))
        return new CloudflareR2StorageService(config);

    var publicBaseUrl = config["Storage:Local:PublicBaseUrl"] ?? "https://localhost:7120";
    return new LocalFileStorageService(webRoot, publicBaseUrl);
});
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgresql");
builder.Services.AddCors(options =>
{
    options.AddPolicy("NutriCasaCors", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200", "https://localhost:4200", "http://localhost:8100", "https://nutricasa.app", "https://www.nutricasa.app"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// HttpClient para Gemini
builder.Services.AddHttpClient("Gemini", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/models/");
    client.DefaultRequestHeaders.TryAddWithoutValidation("x-goog-api-key", config["Gemini:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(config.GetValue<int>("Gemini:TimeoutSeconds", 60));
});

// HttpClient para Resend
builder.Services.AddHttpClient<NutriCasa.Application.Common.Interfaces.IEmailService, NutriCasa.Infrastructure.Services.ResendEmailService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {config["Resend:ApiKey"]}");
});

// JWT Authentication
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, "admin"));
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Seed
await DatabaseSeeder.SeedAsync(app.Services);

// Register recurring jobs
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
HangfireConfiguration.RegisterRecurringJobs(recurringJobManager);

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NutriCasa API v1"));
    app.UseHangfireDashboard("/hangfire");
}

app.UseStaticFiles();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("NutriCasaCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NutriCasa.Api.Hubs.GroupHub>("/hubs/group");
app.MapHealthChecks("/api/health");

Log.Information("NutriCasa API iniciada en {Environment}", app.Environment.EnvironmentName);
app.Run();

// Para tests de integración
namespace NutriCasa.Api
{
    public partial class Program;
}
