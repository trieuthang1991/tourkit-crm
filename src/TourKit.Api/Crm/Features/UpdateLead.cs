using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

using TourKit.Shared.Enums;

namespace TourKit.Api.Crm.Features;

public sealed record UpdateLeadCommand(
    Guid Id, string FullName, string? Phone, string? Email, string? Source,
    LeadStatus Status, Guid? AssignedToUserId) : ICommand<bool>;

public sealed class UpdateLeadValidator : AbstractValidator<UpdateLeadCommand>
{
    public UpdateLeadValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class UpdateLeadHandler : ICommandHandler<UpdateLeadCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateLeadHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateLeadCommand c, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (lead is null)
        {
            return Error.NotFound();
        }

        lead.FullName = c.FullName.Trim();
        lead.Phone = c.Phone;
        lead.Email = c.Email;
        lead.Source = c.Source;
        lead.Status = c.Status;
        lead.AssignedToUserId = c.AssignedToUserId;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
