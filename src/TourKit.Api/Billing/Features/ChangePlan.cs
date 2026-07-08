using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Billing.Features;

public sealed record ChangePlanCommand(string PlanCode) : ICommand<SubscriptionResponse>;

public sealed class ChangePlanValidator : AbstractValidator<ChangePlanCommand>
{
    public ChangePlanValidator()
    {
        RuleFor(x => x.PlanCode).NotEmpty();
    }
}

public sealed class ChangePlanHandler : ICommandHandler<ChangePlanCommand, SubscriptionResponse>
{
    private readonly AppDbContext _db;

    public ChangePlanHandler(AppDbContext db) => _db = db;

    public async Task<Result<SubscriptionResponse>> Handle(ChangePlanCommand c, CancellationToken ct)
    {
        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Code == c.PlanCode, ct);
        if (plan is null)
        {
            return Error.Validation("Gói không tồn tại.");
        }

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(ct);
        if (subscription is null)
        {
            return Error.NotFound();
        }

        subscription.PlanId = plan.Id;
        await _db.SaveChangesAsync(ct);

        return new SubscriptionResponse(
            subscription.Id, subscription.PlanId, plan.Code, subscription.Status,
            subscription.StartedAt, subscription.ExpiresAt);
    }
}
