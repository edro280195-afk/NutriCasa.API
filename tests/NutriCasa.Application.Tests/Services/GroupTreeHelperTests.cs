using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Helpers;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Features.Family.Commands.CreateSubgroup;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;
using NutriCasa.Infrastructure.Persistence;
using NutriCasa.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace NutriCasa.Application.Tests.Services;

public class GroupTreeHelperTests
{
    // --- GroupTreeHelper Tests ---

    [Fact]
    public async Task GetFamilyTreeGroupIds_ReturnsOnlyRoot_WhenNoSubgroupsExist()
    {
        await using var context = CreateContext();
        var rootGroupId = Guid.NewGuid();

        context.Groups.Add(new Group
        {
            Id = rootGroupId,
            Name = "Familia Root",
            InviteCode = "NUT-ROOT-1111",
            GroupType = GroupType.Household
        });
        await context.SaveChangesAsync();

        var result = await GroupTreeHelper.GetFamilyTreeGroupIdsAsync(context, rootGroupId, CancellationToken.None);

        Assert.Single(result);
        Assert.Contains(rootGroupId, result);
    }

    [Fact]
    public async Task GetFamilyTreeGroupIds_ReturnsAllGroupIds_RegardlessOfQueryStartNode()
    {
        await using var context = CreateContext();
        
        var rootId = Guid.NewGuid();
        var level1AId = Guid.NewGuid();
        var level1BId = Guid.NewGuid();
        var level2AId = Guid.NewGuid();

        context.Groups.AddRange(
            new Group
            {
                Id = rootId,
                ParentGroupId = null,
                Name = "Root Family",
                InviteCode = "NUT-ROOT-0000",
                GroupType = GroupType.Household
            },
            new Group
            {
                Id = level1AId,
                ParentGroupId = rootId,
                Name = "Hogar A (Level 1)",
                InviteCode = "NUT-HOGA-1111",
                GroupType = GroupType.Subgroup
            },
            new Group
            {
                Id = level1BId,
                ParentGroupId = rootId,
                Name = "Hogar B (Level 1)",
                InviteCode = "NUT-HOGB-2222",
                GroupType = GroupType.Subgroup
            },
            new Group
            {
                Id = level2AId,
                ParentGroupId = level1AId,
                Name = "Hogar A-Sub (Level 2)",
                InviteCode = "NUT-SUB2-3333",
                GroupType = GroupType.Subgroup
            }
        );
        await context.SaveChangesAsync();

        // Test querying starting from Root
        var resultFromRoot = await GroupTreeHelper.GetFamilyTreeGroupIdsAsync(context, rootId, CancellationToken.None);
        Assert.Equal(4, resultFromRoot.Count);
        Assert.Contains(rootId, resultFromRoot);
        Assert.Contains(level1AId, resultFromRoot);
        Assert.Contains(level1BId, resultFromRoot);
        Assert.Contains(level2AId, resultFromRoot);

        // Test querying starting from Level 1 subgroup
        var resultFromL1 = await GroupTreeHelper.GetFamilyTreeGroupIdsAsync(context, level1AId, CancellationToken.None);
        Assert.Equal(4, resultFromL1.Count);
        Assert.Contains(rootId, resultFromL1);
        Assert.Contains(level1AId, resultFromL1);
        Assert.Contains(level1BId, resultFromL1);
        Assert.Contains(level2AId, resultFromL1);

        // Test querying starting from Level 2 subgroup
        var resultFromL2 = await GroupTreeHelper.GetFamilyTreeGroupIdsAsync(context, level2AId, CancellationToken.None);
        Assert.Equal(4, resultFromL2.Count);
        Assert.Contains(rootId, resultFromL2);
        Assert.Contains(level1AId, resultFromL2);
        Assert.Contains(level1BId, resultFromL2);
        Assert.Contains(level2AId, resultFromL2);
    }

    // --- CreateSubgroupCommandHandler Tests ---

    [Fact]
    public async Task CreateSubgroup_SuccessfullyCreatesSubgroup_AndMarksParentMembershipAsLeft()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var parentGroupId = Guid.NewGuid();

        var parentGroup = new Group
        {
            Id = parentGroupId,
            Name = "Familia Principal",
            InviteCode = "NUT-PRIN-1111",
            GroupType = GroupType.Household
        };

        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = parentGroupId,
            UserId = userId,
            Role = GroupRole.Owner,
            JoinedAt = DateTime.UtcNow.AddMonths(-1),
            LeftAt = null,
            Group = parentGroup
        };

        context.Groups.Add(parentGroup);
        context.GroupMemberships.Add(membership);
        await context.SaveChangesAsync();

        var handler = new CreateSubgroupCommandHandler(context, new TestCurrentUserService(userId));

        var command = new CreateSubgroupCommand
        {
            Name = "Mi Hogar Subgrupo",
            Description = "Subgrupo para mi hogar"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.SubgroupId);

        // Verify the parent membership was marked as left
        var updatedParentMembership = await context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == parentGroupId && m.UserId == userId);
        
        Assert.NotNull(updatedParentMembership);
        Assert.NotNull(updatedParentMembership.LeftAt);

        // Verify the new subgroup was created and the user is its owner
        var subgroup = await context.Groups.FindAsync(result.Value.SubgroupId);
        Assert.NotNull(subgroup);
        Assert.Equal(parentGroupId, subgroup.ParentGroupId);
        Assert.Equal("Mi Hogar Subgrupo", subgroup.Name);
        Assert.Equal(GroupType.Subgroup, subgroup.GroupType);

        var subgroupMembership = await context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == subgroup.Id && m.UserId == userId);
        
        Assert.NotNull(subgroupMembership);
        Assert.Equal(GroupRole.Owner, subgroupMembership.Role);
        Assert.Null(subgroupMembership.LeftAt);
    }

    [Fact]
    public async Task CreateSubgroup_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        await using var context = CreateContext();
        var handler = new CreateSubgroupCommandHandler(context, new TestCurrentUserService(null));

        var result = await handler.Handle(new CreateSubgroupCommand { Name = "Nuevo Grupo" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.ErrorCode);
    }

    [Fact]
    public async Task CreateSubgroup_ReturnsNoGroup_WhenUserHasNoActiveMembership()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var handler = new CreateSubgroupCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new CreateSubgroupCommand { Name = "Nuevo Grupo" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NO_GROUP", result.ErrorCode);
    }

    [Fact]
    public async Task CreateSubgroup_ReturnsForbidden_WhenUserIsNotOwnerOrAdmin()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var parentGroupId = Guid.NewGuid();

        var parentGroup = new Group
        {
            Id = parentGroupId,
            Name = "Familia Principal",
            InviteCode = "NUT-PRIN-1111",
            GroupType = GroupType.Household
        };

        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = parentGroupId,
            UserId = userId,
            Role = GroupRole.Member, // Not Owner/Admin
            JoinedAt = DateTime.UtcNow.AddMonths(-1),
            LeftAt = null,
            Group = parentGroup
        };

        context.Groups.Add(parentGroup);
        context.GroupMemberships.Add(membership);
        await context.SaveChangesAsync();

        var handler = new CreateSubgroupCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new CreateSubgroupCommand { Name = "Nuevo Grupo" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task CreateSubgroup_ReturnsMaxDepth_WhenGroupDepthIsAlreadyTwoOrMore()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        
        var rootGroupId = Guid.NewGuid();
        var level1Id = Guid.NewGuid();
        var level2Id = Guid.NewGuid();

        var rootGroup = new Group
        {
            Id = rootGroupId,
            Name = "Root Group",
            InviteCode = "NUT-ROOT-1111",
            GroupType = GroupType.Household
        };

        var level1Group = new Group
        {
            Id = level1Id,
            ParentGroupId = rootGroupId,
            Name = "Level 1 Subgroup",
            InviteCode = "NUT-LEV1-1111",
            GroupType = GroupType.Subgroup
        };

        var level2Group = new Group
        {
            Id = level2Id,
            ParentGroupId = level1Id,
            Name = "Level 2 Subgroup",
            InviteCode = "NUT-LEV2-1111",
            GroupType = GroupType.Subgroup
        };

        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = level2Id,
            UserId = userId,
            Role = GroupRole.Owner,
            JoinedAt = DateTime.UtcNow.AddMonths(-1),
            LeftAt = null,
            Group = level2Group
        };

        context.Groups.AddRange(rootGroup, level1Group, level2Group);
        context.GroupMemberships.Add(membership);
        await context.SaveChangesAsync();

        var handler = new CreateSubgroupCommandHandler(context, new TestCurrentUserService(userId));

        var result = await handler.Handle(new CreateSubgroupCommand { Name = "Level 3 Subgroup" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("MAX_DEPTH", result.ErrorCode);
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
