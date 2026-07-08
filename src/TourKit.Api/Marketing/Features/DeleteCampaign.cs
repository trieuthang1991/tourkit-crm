using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Marketing.Features;

public sealed record DeleteCampaignCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteCampaignHandler : ICommandHandler<DeleteCampaignCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteCampaignHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteCampaignCommand c, CancellationToken ct)
    {
        var campaign = await _db.MarketingCampaigns.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (campaign is null)
        {
            return Error.NotFound();
        }

        campaign.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
