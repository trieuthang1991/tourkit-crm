using FluentValidation;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission.Features;

public sealed record CreateCommissionRuleCommand(Guid UserId, decimal Percentage, int Status)
    : ICommand<CommissionRuleResponse>;

public sealed class CreateCommissionRuleValidator : AbstractValidator<CreateCommissionRuleCommand>
{
    public CreateCommissionRuleValidator()
    {
        RuleFor(x => x.Percentage).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateCommissionRuleHandler : ICommandHandler<CreateCommissionRuleCommand, CommissionRuleResponse>
{
    private readonly AppDbContext _db;

    public CreateCommissionRuleHandler(AppDbContext db) => _db = db;

    public async Task<Result<CommissionRuleResponse>> Handle(CreateCommissionRuleCommand c, CancellationToken ct)
    {
        var rule = new CommissionRule { UserId = c.UserId, Percentage = c.Percentage, Status = c.Status };
        _db.CommissionRules.Add(rule);
        await _db.SaveChangesAsync(ct);

        return new CommissionRuleResponse(rule.Id, rule.UserId, rule.Percentage, rule.Status);
    }
}
