using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

using TourKit.Shared.Enums;

namespace TourKit.Api.Marketing.Features;

public sealed record UpdateCampaignCommand(
    Guid Id, string Name, MarketingChannel Channel, string? Subject, string Body, int Status)
    : ICommand<bool>;

public sealed class UpdateCampaignValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
    }
}

public sealed class UpdateCampaignHandler : ICommandHandler<UpdateCampaignCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateCampaignHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateCampaignCommand c, CancellationToken ct)
    {
        var campaign = await _db.MarketingCampaigns.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (campaign is null)
        {
            return Error.NotFound();
        }

        campaign.Name = c.Name.Trim();
        campaign.Channel = c.Channel;
        campaign.Subject = c.Subject;
        campaign.Body = c.Body;
        campaign.Status = c.Status;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
