using FluentValidation;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record CreateLeadCommand(
    string FullName, string? Phone, string? Email, string? Source, Guid? AssignedToUserId) : ICommand<LeadResponse>;

public sealed class CreateLeadValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class CreateLeadHandler : ICommandHandler<CreateLeadCommand, LeadResponse>
{
    private readonly AppDbContext _db;

    public CreateLeadHandler(AppDbContext db) => _db = db;

    public async Task<Result<LeadResponse>> Handle(CreateLeadCommand c, CancellationToken ct)
    {
        var lead = new Lead
        {
            FullName = c.FullName.Trim(), Phone = c.Phone, Email = c.Email,
            Source = c.Source, AssignedToUserId = c.AssignedToUserId,
        };
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);

        return new LeadResponse(
            lead.Id, lead.FullName, lead.Phone, lead.Email, lead.Source,
            lead.Status, lead.AssignedToUserId, lead.ConvertedCustomerId);
    }
}
