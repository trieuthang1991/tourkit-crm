using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission.Features;

public sealed record CreateProfitShareCommand(Guid OrderId, Guid UserId, decimal Percentage)
    : ICommand<ProfitShareResponse>;

public sealed class CreateProfitShareValidator : AbstractValidator<CreateProfitShareCommand>
{
    public CreateProfitShareValidator()
    {
        RuleFor(x => x.Percentage).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public sealed class CreateProfitShareHandler : ICommandHandler<CreateProfitShareCommand, ProfitShareResponse>
{
    private readonly AppDbContext _db;

    public CreateProfitShareHandler(AppDbContext db) => _db = db;

    public async Task<Result<ProfitShareResponse>> Handle(CreateProfitShareCommand c, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == c.OrderId, ct);
        if (order is null)
        {
            return Error.NotFound();
        }

        var profit = OrderMath.Profit(order);
        var amount = CommissionMath.ShareAmount(profit, c.Percentage);

        var share = new ProfitShare
        {
            OrderId = c.OrderId,
            UserId = c.UserId,
            Percentage = c.Percentage,
            Amount = amount,
            ProfitBase = profit,
        };
        _db.ProfitShares.Add(share);
        await _db.SaveChangesAsync(ct);

        return new ProfitShareResponse(
            share.Id, share.OrderId, share.UserId, share.Percentage, share.Amount, share.ProfitBase);
    }
}
