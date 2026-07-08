using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record CreateOrderCostCommand(
    Guid OrderId, Guid ProviderId, string? ServiceName, int DayIndex, decimal ExpectedAmount,
    decimal ActualAmount, decimal Deposit, decimal Surcharge, decimal Vat, int Status)
    : ICommand<OrderCostResponse>;

public sealed class CreateOrderCostValidator : AbstractValidator<CreateOrderCostCommand>
{
    public CreateOrderCostValidator()
    {
        RuleFor(x => x.ActualAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateOrderCostHandler : ICommandHandler<CreateOrderCostCommand, OrderCostResponse>
{
    private readonly AppDbContext _db;

    public CreateOrderCostHandler(AppDbContext db) => _db = db;

    public async Task<Result<OrderCostResponse>> Handle(CreateOrderCostCommand c, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == c.OrderId, ct);
        if (order is null)
        {
            return Error.NotFound();
        }

        var providerExists = await _db.Providers.AnyAsync(p => p.Id == c.ProviderId, ct);
        if (!providerExists)
        {
            return Error.Validation("Nhà cung cấp không tồn tại.");
        }

        var cost = new OrderCost
        {
            OrderId = c.OrderId,
            ProviderId = c.ProviderId,
            ServiceName = c.ServiceName,
            DayIndex = c.DayIndex,
            ExpectedAmount = c.ExpectedAmount,
            ActualAmount = c.ActualAmount,
            Deposit = c.Deposit,
            Surcharge = c.Surcharge,
            Vat = c.Vat,
            Status = c.Status,
        };
        _db.OrderCosts.Add(cost);

        // Recompute Order.TotalCost = tổng ActualAmount toàn bộ dòng chi phí của đơn (kể cả dòng mới).
        var existingCosts = await _db.OrderCosts.AsNoTracking()
            .Where(x => x.OrderId == c.OrderId).ToListAsync(ct);
        order.TotalCost = OrderMath.TotalCost(existingCosts.Append(cost));

        await _db.SaveChangesAsync(ct);

        return new OrderCostResponse(
            cost.Id, cost.OrderId, cost.ProviderId, cost.ServiceName, cost.DayIndex,
            cost.ExpectedAmount, cost.ActualAmount, cost.Deposit, cost.Surcharge, cost.Vat, cost.Status);
    }
}
