using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission.Features;

public sealed record DeleteCommissionRuleCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteCommissionRuleHandler : ICommandHandler<DeleteCommissionRuleCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteCommissionRuleHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteCommissionRuleCommand c, CancellationToken ct)
    {
        var rule = await _db.CommissionRules.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (rule is null)
        {
            return Error.NotFound();
        }

        rule.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
