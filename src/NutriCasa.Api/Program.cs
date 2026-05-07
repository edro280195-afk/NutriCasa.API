using Hangfire;
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
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
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
