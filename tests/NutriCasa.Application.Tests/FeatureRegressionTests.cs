using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep1Group;
using NutriCasa.Application.Features.Subscriptions.Commands.CreateSubscription;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;
using NutriCasa.Infrastructure.Persistence;
using NutriCasa.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace NutriCasa.Application.Tests;

public class FeatureRegressionTests
{
    [Fact]
    public async Task JoinGroup_ReturnsLimitError_WhenOwnerPlanMemberLimitIsReached()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var joiningUserId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        context.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Code = "family",
            Name = "Familiar",
            PriceMonthlyMxn = 199,
            MaxGroupMembers = 2,
            Features = "{}",
            IsActive = true,
        });
        context.Groups.Add(new Group
        {
            Id = groupId,
            Name = "Casa",
            InviteCode = "NUT-TEST-1234",
            CreatedByUserId = ownerId,
        });
        context.GroupMemberships.AddRange(
            new GroupMembership
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = ownerId,
                Role = GroupRole.Owner,
                JoinedAt = DateTime.UtcNow,
            },
            new GroupMembership
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = existingMemberId,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow,
            });
        context.UserSubscriptions.Add(new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        await context.SaveChangesAsync();

        var handler = new CompleteStep1GroupCommandHandler(
            context,
            new TestCurrentUserService(joiningUserId));

        var result = await handler.Handle(new CompleteStep1GroupCommand
        {
            Action = "join",
            InviteCode = "nut-test-1234",
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("GROUP_MEMBER_LIMIT_REACHED", result.ErrorCode);
        Assert.Equal(2, await context.GroupMemberships.CountAsync(m => m.GroupId == groupId));
    }

    [Fact]
    public async Task CreateSubscription_CreatesPendingSubscription_WithCheckoutUrl_ForPaidPlan()
    {
        var now = new DateTime(2026, 5, 19, 12, 0, 0, DateTimeKind.Utc);
        await using var context = CreateContext(now);
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        context.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Code = "pro",
            Name = "Pro",
            PriceMonthlyMxn = 149,
            TrialDays = 0,
            MaxGroupMembers = 8,
            Features = "{}",
            IsActive = true,
        });
        await context.SaveChangesAsync();

        var paymentService = new TestPaymentService(
            new PaymentCheckoutResult("pref_123", "https://pay.test/checkout"));
        var handler = new CreateSubscriptionCommandHandler(
            context,
            new TestCurrentUserService(userId),
            new TestDateTimeService(now),
            paymentService);

        var result = await handler.Handle(new CreateSubscriptionCommand
        {
            PlanId = planId,
            IsTrial = false,
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value!.Status);
        Assert.Equal("https://pay.test/checkout", result.Value.CheckoutUrl);

        var subscription = await context.UserSubscriptions.SingleAsync();
        Assert.Equal(SubscriptionStatus.Pending, subscription.Status);
        Assert.Equal("mercadopago", subscription.PaymentProvider);
        Assert.Equal("pref_123", subscription.ProviderSubscriptionId);
        Assert.Contains("https://pay.test/checkout", subscription.Metadata);
    }

    private static ApplicationDbContext CreateContext(DateTime? now = null)
    {
        var currentUser = new TestCurrentUserService(Guid.NewGuid());
        var dateTime = new TestDateTimeService(now ?? DateTime.UtcNow);
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

    private sealed class TestPaymentService : IPaymentService
    {
        private readonly PaymentCheckoutResult _checkoutResult;

        public TestPaymentService(PaymentCheckoutResult checkoutResult)
        {
            _checkoutResult = checkoutResult;
        }

        public Task<PaymentCheckoutResult> CreateCheckoutSessionAsync(Guid userId, Guid planId, string returnUrl, CancellationToken ct = default)
            => Task.FromResult(_checkoutResult);

        public Task<string> CreateTrialSubscriptionAsync(Guid userId, Guid planId, CancellationToken ct = default)
            => Task.FromResult($"trial_{userId:N}_{planId:N}");

        public Task CancelSubscriptionAsync(string providerSubscriptionId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<bool> VerifyPaymentAsync(string providerPaymentId, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}
