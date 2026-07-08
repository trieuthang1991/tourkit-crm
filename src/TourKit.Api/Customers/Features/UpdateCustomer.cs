using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Customers.Features;

public sealed record UpdateCustomerCommand(Guid Id, string FullName, string? Phone) : ICommand<bool>;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateCustomerHandler : ICommandHandler<UpdateCustomerCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateCustomerHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateCustomerCommand c, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (customer is null)
        {
            return Error.NotFound();
        }

        customer.FullName = c.FullName.Trim();
        customer.Phone = c.Phone;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
