using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NutriCasa.Application;
using NutriCasa.Infrastructure;
using NutriCasa.Infrastructure.Persistence.Seeds;
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
builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("Gemini:TimeoutSeconds", 60));
});

// HttpClient para Resend
builder.Services.AddHttpClient<NutriCasa.Application.Common.Interfaces.IEmailService, NutriCasa.Infrastructure.Services.ResendEmailService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {config["Resend:ApiKey"]}");
});

// JWT Authentication
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
});

var app = builder.Build();

// Seed
await DatabaseSeeder.SeedAsync(app.Services);

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NutriCasa API v1"));
    app.UseHangfireDashboard("/hangfire");
}

app.UseSerilogRequestLogging();
app.UseCors("NutriCasaCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");

Log.Information("NutriCasa API iniciada en {Environment}", app.Environment.EnvironmentName);
app.Run();

// Para tests de integración
namespace NutriCasa.Api
{
    public partial class Program;
}
