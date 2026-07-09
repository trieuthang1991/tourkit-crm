using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record CreateProviderCommand(
    string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, int Rate, int Status)
    : ICommand<ProviderResponse>;

public sealed class CreateProviderValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateProviderHandler : ICommandHandler<CreateProviderCommand, ProviderResponse>
{
    private readonly AppDbContext _db;

    public CreateProviderHandler(AppDbContext db) => _db = db;

    public async Task<Result<ProviderResponse>> Handle(CreateProviderCommand c, CancellationToken ct)
    {
        var code = c.Code.Trim();
        if (await _db.Providers.AnyAsync(p => p.Code == code, ct))
        {
            return Error.Conflict($"Mã nhà cung cấp '{code}' đã tồn tại.");
        }

        var provider = new Provider
        {
            Code = code,
            Name = c.Name.Trim(),
            Type = c.Type,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            TaxCode = c.TaxCode,
            ContactPerson = c.ContactPerson,
            BankAccount = c.BankAccount,
            BankName = c.BankName,
            Rate = c.Rate,
            Status = c.Status,
        };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(ct);

        return new ProviderResponse(
            provider.Id, provider.Code, provider.Name, provider.Type, provider.Phone, provider.Email,
            provider.Address, provider.TaxCode, provider.ContactPerson, provider.BankAccount,
            provider.BankName, provider.Rate, provider.Status);
    }
}
