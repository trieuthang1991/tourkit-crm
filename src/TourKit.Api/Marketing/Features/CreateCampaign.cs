using FluentValidation;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Marketing.Features;

public sealed record CreateCampaignCommand(string Name, MarketingChannel Channel, string? Subject, string Body)
    : ICommand<CampaignResponse>;

public sealed class CreateCampaignValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
    }
}

public sealed class CreateCampaignHandler : ICommandHandler<CreateCampaignCommand, CampaignResponse>
{
    private readonly AppDbContext _db;

    public CreateCampaignHandler(AppDbContext db) => _db = db;

    public async Task<Result<CampaignResponse>> Handle(CreateCampaignCommand c, CancellationToken ct)
    {
        var campaign = new MarketingCampaign
        {
            Name = c.Name.Trim(), Channel = c.Channel,
            Subject = c.Subject, Body = c.Body,
        };
        _db.MarketingCampaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);

        return new CampaignResponse(
            campaign.Id, campaign.Name, campaign.Channel, campaign.Subject, campaign.Body, campaign.Status);
    }
}
