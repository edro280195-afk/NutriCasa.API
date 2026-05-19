using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public static class HangfireConfiguration
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        return services;
    }

    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<AccountDeletionPurgeJob>(
            "account-deletion-purge",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily);

        RecurringJob.AddOrUpdate<WeeklyDifficultyAnalysisJob>(
            "weekly-difficulty-analysis",
            job => job.RunAsync(CancellationToken.None),
            Cron.Weekly(DayOfWeek.Sunday, 23));

        RecurringJob.AddOrUpdate<CheckInReminderJob>(
            "check-in-reminder",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(20));

        RecurringJob.AddOrUpdate<InviteCodeExpiryJob>(
            "invite-code-expiry",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily);

        RecurringJob.AddOrUpdate<MilestoneDetectionJob>(
            "milestone-detection",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily);

        RecurringJob.AddOrUpdate<MotivationPostJob>(
            "motivation-post-mon",
            job => job.RunAsync(CancellationToken.None),
            Cron.Weekly(DayOfWeek.Monday, 10));

        RecurringJob.AddOrUpdate<MotivationPostJob>(
            "motivation-post-thu",
            job => job.RunAsync(CancellationToken.None),
            Cron.Weekly(DayOfWeek.Thursday, 18));
    }
}
