using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission.Features;

public sealed record UpdateCommissionRuleCommand(Guid Id, decimal Percentage, int Status) : ICommand<bool>;

public sealed class UpdateCommissionRuleValidator : AbstractValidator<UpdateCommissionRuleCommand>
{
    public UpdateCommissionRuleValidator()
    {
        RuleFor(x => x.Percentage).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateCommissionRuleHandler : ICommandHandler<UpdateCommissionRuleCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateCommissionRuleHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateCommissionRuleCommand c, CancellationToken ct)
    {
        var rule = await _db.CommissionRules.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (rule is null)
        {
            return Error.NotFound();
        }

        rule.Percentage = c.Percentage;
        rule.Status = c.Status;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
