using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record UpdateServiceItemCommand(Guid Id, string Name, int Category, int Status) : ICommand<bool>;

public sealed class UpdateServiceItemValidator : AbstractValidator<UpdateServiceItemCommand>
{
    public UpdateServiceItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public sealed class UpdateServiceItemHandler : ICommandHandler<UpdateServiceItemCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateServiceItemHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateServiceItemCommand c, CancellationToken ct)
    {
        var serviceItem = await _db.ServiceItems.FirstOrDefaultAsync(s => s.Id == c.Id, ct);
        if (serviceItem is null)
        {
            return Error.NotFound();
        }

        serviceItem.Name = c.Name.Trim();
        serviceItem.Category = c.Category;
        serviceItem.Status = c.Status;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
