using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Features.Challenges.Commands.CreateChallenge;
using NutriCasa.Application.Features.Challenges.Commands.JoinChallenge;
using NutriCasa.Application.Features.Challenges.Commands.LeaveChallenge;
using NutriCasa.Application.Features.Challenges.Services;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;
using NutriCasa.Infrastructure.Persistence;
using NutriCasa.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace NutriCasa.Application.Tests.Services;

public class ChallengeTests
{
    // --- ChallengeScoringService Tests ---

    [Fact]
    public void CalculateScore_MostWeightLoss_ReturnsPercentageLoss()
    {
        var score = ChallengeScoringService.CalculateScore(
            ChallengeGoalType.MostWeightLoss,
            startingValue: 100m,
            currentValue: 90m,
            longestStreak: 0,
            totalCheckIns: 0,
            adherencePercent: 0);

        Assert.Equal(10m, score); // 10% weight loss
    }

    [Fact]
    public void CalculateScore_BestStreak_ReturnsStreak()
    {
        var score = ChallengeScoringService.CalculateScore(
            ChallengeGoalType.BestStreak,
            startingValue: null,
            currentValue: null,
            longestStreak: 7,
            totalCheckIns: 0,
            adherencePercent: 0);

        Assert.Equal(7m, score);
    }

    [Fact]
    public void FormatValue_MostWeightLoss_ReturnsFormattedLoss()
    {
        var display = ChallengeScoringService.FormatValue(
            ChallengeGoalType.MostWeightLoss,
            startingValue: 80m,
            currentValue: 76m,
            longestStreak: 0,
            totalCheckIns: 0,
            adherencePercent: 0);

        Assert.Equal("4.0 kg (5.0%)", display);
    }

    // --- CreateChallengeCommandHandler Tests ---

    [Fact]
    public async Task CreateChallenge_ReturnsFailure_WhenUserNotAuthenticated()
    {
        await using var context = CreateContext();
        var handler = new CreateChallengeCommandHandler(context, new TestCurrentUserService(null));

        var result = await handler.Handle(new CreateChallengeCommand
        {
            Title = "Reto Test",
            GoalType = "MostWeightLoss",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.ErrorCode);
    }

    [Fact]
    public async Task CreateChallenge_ReturnsFailure_WhenDatesAreInvalid()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var handler = new CreateChallengeCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new CreateChallengeCommand
        {
            Title = "Reto Test",
            GoalType = "MostWeightLoss",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow) // start > end
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_DATES", result.ErrorCode);
    }

    [Fact]
    public async Task CreateChallenge_ReturnsFailure_WhenUserNotInGroup()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var handler = new CreateChallengeCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new CreateChallengeCommand
        {
            Title = "Reto Test",
            GoalType = "MostWeightLoss",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NO_GROUP", result.ErrorCode);
    }

    [Fact]
    public async Task CreateChallenge_ReturnsFailure_WhenUserNotAdminOrOwner()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        context.GroupMemberships.Add(new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
            Role = GroupRole.Member, // Member cannot create challenges
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new CreateChallengeCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new CreateChallengeCommand
        {
            Title = "Reto Test",
            GoalType = "MostWeightLoss",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task CreateChallenge_CreatesChallengeAndJoinsCreator_WhenSuccess()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        context.GroupMemberships.Add(new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
            Role = GroupRole.Owner,
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new CreateChallengeCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new CreateChallengeCommand
        {
            Title = "Reto Test",
            GoalType = "MostWeightLoss",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Reto Test", result.Value.Title);

        var challenge = await context.Challenges
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == result.Value.ChallengeId);

        Assert.NotNull(challenge);
        Assert.Equal(groupId, challenge.GroupId);
        Assert.Single(challenge.Participants);
        Assert.Equal(userId, challenge.Participants.First().UserId);
    }

    // --- JoinChallengeCommandHandler Tests ---

    [Fact]
    public async Task JoinChallenge_ReturnsFailure_WhenChallengeDoesNotExist()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var handler = new JoinChallengeCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new JoinChallengeCommand
        {
            ChallengeId = Guid.NewGuid()
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task JoinChallenge_ReturnsFailure_WhenUserNotInSameGroup()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var challengeGroupId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();

        context.Challenges.Add(new Challenge
        {
            Id = challengeId,
            GroupId = challengeGroupId,
            Title = "Reto de peso",
            GoalType = ChallengeGoalType.MostWeightLoss,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            IsActive = true
        });

        context.GroupMemberships.Add(new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = userGroupId, // user in different group
            UserId = userId,
            Role = GroupRole.Member,
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new JoinChallengeCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new JoinChallengeCommand
        {
            ChallengeId = challengeId
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NO_GROUP", result.ErrorCode);
    }

    [Fact]
    public async Task JoinChallenge_AddsParticipant_WhenSuccess()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        context.Challenges.Add(new Challenge
        {
            Id = challengeId,
            GroupId = groupId,
            Title = "Reto de peso",
            GoalType = ChallengeGoalType.MostWeightLoss,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            IsActive = true
        });

        context.GroupMemberships.Add(new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
            Role = GroupRole.Member,
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new JoinChallengeCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new JoinChallengeCommand
        {
            ChallengeId = challengeId
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var participantExists = await context.ChallengeParticipants
            .AnyAsync(p => p.ChallengeId == challengeId && p.UserId == userId);

        Assert.True(participantExists);
    }

    // --- LeaveChallengeCommandHandler Tests ---

    [Fact]
    public async Task LeaveChallenge_RemovesParticipant_WhenSuccess()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();

        context.ChallengeParticipants.Add(new ChallengeParticipant
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new LeaveChallengeCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new LeaveChallengeCommand
        {
            ChallengeId = challengeId
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var participantExists = await context.ChallengeParticipants
            .AnyAsync(p => p.ChallengeId == challengeId && p.UserId == userId);

        Assert.False(participantExists);
    }

    // --- Helpers ---

    private static ApplicationDbContext CreateContext()
    {
        var currentUser = new TestCurrentUserService(Guid.NewGuid());
        var dateTime = new TestDateTimeService(DateTime.UtcNow);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(
            options,
            new AuditableEntityInterceptor(dateTime, currentUser),
            new SoftDeleteInterceptor(dateTime));
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public TestCurrentUserService(Guid? userId)
        {
            UserId = userId;
        }

        public Guid? UserId { get; }
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "tests";
        public bool IsAuthenticated => UserId is not null;
    }

    private sealed class TestDateTimeService : IDateTimeService
    {
        public TestDateTimeService(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
