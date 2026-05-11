using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Subscriptions.DTOs;

namespace NutriCasa.Application.Features.Subscriptions.Queries.GetPlans;

public record GetPlansQuery : IRequest<Result<List<SubscriptionPlanDto>>>;

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, Result<List<SubscriptionPlanDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPlansQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<SubscriptionPlanDto>>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new SubscriptionPlanDto
            {
                PlanId = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                PriceMonthlyMxn = p.PriceMonthlyMxn,
                PriceYearlyMxn = p.PriceYearlyMxn,
                TrialDays = p.TrialDays,
                MaxGroupMembers = p.MaxGroupMembers,
                MaxRegenerationsWeek = p.MaxRegenerationsWeek,
                MaxSwapsWeek = p.MaxSwapsWeek,
                HasAiChat = p.HasAiChat,
                HasPhotoAnalysis = p.HasPhotoAnalysis,
                SortOrder = p.SortOrder,
            })
            .ToListAsync(cancellationToken);

        return Result<List<SubscriptionPlanDto>>.Success(plans);
    }
}
