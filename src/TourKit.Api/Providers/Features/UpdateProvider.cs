using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

using TourKit.Shared.Enums;

namespace TourKit.Api.Providers.Features;

public sealed record UpdateProviderCommand(
    Guid Id, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, int Rate, int Status)
    : ICommand<bool>;

public sealed class UpdateProviderValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateProviderHandler : ICommandHandler<UpdateProviderCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateProviderHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateProviderCommand c, CancellationToken ct)
    {
        var provider = await _db.Providers.FirstOrDefaultAsync(p => p.Id == c.Id, ct);
        if (provider is null)
        {
            return Error.NotFound();
        }

        provider.Name = c.Name.Trim();
        provider.Type = c.Type;
        provider.Phone = c.Phone;
        provider.Email = c.Email;
        provider.Address = c.Address;
        provider.TaxCode = c.TaxCode;
        provider.ContactPerson = c.ContactPerson;
        provider.BankAccount = c.BankAccount;
        provider.BankName = c.BankName;
        provider.Rate = c.Rate;
        provider.Status = c.Status;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
