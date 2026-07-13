using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Crm;

public sealed class LeadCampaignServiceTests
{
    private static LeadCampaignService NewService(FakeRepository<LeadCampaign> repo, FakeRepository<Lead> leadRepo)
        => new(repo, leadRepo, new FakeRepository<User>());

    [Fact]
    public async Task CreateAsync_rejects_empty_name()
    {
        var service = NewService(new FakeRepository<LeadCampaign>(), new FakeRepository<Lead>());
        await Assert.ThrowsAsync<TourKit.Application.Common.ValidationAppException>(
            () => service.CreateAsync(new CreateLeadCampaignDto("  ", null)));
    }

    [Fact]
    public async Task List_computes_progress_and_close_rate_from_leads()
    {
        var repo = new FakeRepository<LeadCampaign>();
        var leadRepo = new FakeRepository<Lead>();
        var service = NewService(repo, leadRepo);

        var campaign = await service.CreateAsync(new CreateLeadCampaignDto("CD1", null));
        // 4 lead: 1 New, 1 Contacted, 1 Qualified, 1 Won → cared=3/4=75%, closed=1/4=25%
        foreach (var st in new[] { LeadStatus.New, LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Won })
        {
            await leadRepo.AddAsync(new Lead { FullName = "L", Status = st, CampaignId = campaign.Id });
        }
        await leadRepo.SaveChangesAsync();

        var row = Assert.Single((await service.ListAsync(1, 20)).Items);
        Assert.Equal(4, row.TotalLeads);
        Assert.Equal(3, row.CaredCount);
        Assert.Equal(1, row.ClosedCount);
        Assert.Equal(75m, row.Progress);
        Assert.Equal(25m, row.CloseRate);
    }

    [Fact]
    public async Task Stats_counts_campaigns_leads_and_completed()
    {
        var repo = new FakeRepository<LeadCampaign>();
        var leadRepo = new FakeRepository<Lead>();
        var service = NewService(repo, leadRepo);

        var c1 = await service.CreateAsync(new CreateLeadCampaignDto("A", null));
        await service.CreateAsync(new CreateLeadCampaignDto("B", null));
        await leadRepo.AddAsync(new Lead { FullName = "x", Status = LeadStatus.Won, CampaignId = c1.Id });
        await leadRepo.AddAsync(new Lead { FullName = "y", Status = LeadStatus.New, CampaignId = c1.Id });
        await leadRepo.SaveChangesAsync();

        var stats = await service.GetStatsAsync();
        Assert.Equal(2, stats.TotalCampaigns);
        Assert.Equal(2, stats.TotalLeads);
        Assert.Equal(50m, stats.AvgCloseRate); // 1 Won / 2 leads
    }
}
