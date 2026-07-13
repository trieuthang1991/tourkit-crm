using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;
using TourKit.Application.B2B.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using TourKit.UnitTests.Booking; // FakeRepository<T> generic dùng chung

namespace TourKit.UnitTests.B2B;

public sealed class AgentQuoteRequestServiceTests
{
    private static async Task<(AgentQuoteRequestService Svc, Guid AgentId)> NewServiceAsync()
    {
        var repo = new FakeRepository<AgentQuoteRequest>();
        var agentRepo = new FakeRepository<Agent>();
        var agentId = Guid.NewGuid();
        await agentRepo.AddAsync(new Agent { Id = agentId, Code = "A1", Name = "Đại lý 1" });
        await agentRepo.SaveChangesAsync();
        var svc = new AgentQuoteRequestService(repo, agentRepo, new CreateAgentQuoteRequestValidator());
        return (svc, agentId);
    }

    private static CreateAgentQuoteRequestDto Req(Guid agentId) =>
        new(agentId, "Tour Đà Nẵng 3N2Đ", null, null, 10, "Phòng view biển");

    [Fact]
    public async Task CreateAsync_unknown_agent_throws_Validation()
    {
        var (svc, _) = await NewServiceAsync();

        await Assert.ThrowsAsync<ValidationAppException>(() => svc.CreateAsync(Req(Guid.NewGuid())));
    }

    [Fact]
    public async Task Full_workflow_request_quote_confirm()
    {
        var (svc, agentId) = await NewServiceAsync();

        var created = await svc.CreateAsync(Req(agentId));
        Assert.Equal(AgentQuoteStatus.Requested, created.Status);

        var quoted = await svc.QuoteAsync(created.Id, new QuoteAgentRequestDto(50_000_000m, "Đã bao gồm VAT"));
        Assert.Equal(AgentQuoteStatus.Quoted, quoted.Status);
        Assert.Equal(50_000_000m, quoted.QuotedAmount);

        var confirmed = await svc.ConfirmAsync(created.Id);
        Assert.Equal(AgentQuoteStatus.Confirmed, confirmed.Status);
    }

    [Fact]
    public async Task ConfirmAsync_before_quote_throws_Conflict()
    {
        var (svc, agentId) = await NewServiceAsync();
        var created = await svc.CreateAsync(Req(agentId));

        await Assert.ThrowsAsync<ConflictException>(() => svc.ConfirmAsync(created.Id));
    }

    [Fact]
    public async Task QuoteAsync_twice_throws_Conflict()
    {
        var (svc, agentId) = await NewServiceAsync();
        var created = await svc.CreateAsync(Req(agentId));
        await svc.QuoteAsync(created.Id, new QuoteAgentRequestDto(1m, null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.QuoteAsync(created.Id, new QuoteAgentRequestDto(2m, null)));
    }

    [Fact]
    public async Task RejectAsync_after_confirm_throws_Conflict()
    {
        var (svc, agentId) = await NewServiceAsync();
        var created = await svc.CreateAsync(Req(agentId));
        await svc.QuoteAsync(created.Id, new QuoteAgentRequestDto(1m, null));
        await svc.ConfirmAsync(created.Id);

        await Assert.ThrowsAsync<ConflictException>(() => svc.RejectAsync(created.Id, "muộn"));
    }

    [Fact]
    public async Task ListAsync_filters_by_agent()
    {
        var (svc, agentId) = await NewServiceAsync();
        await svc.CreateAsync(Req(agentId));

        var byAgent = await svc.ListAsync(1, 20, new AgentQuoteRequestListFilter(AgentId: agentId));
        Assert.Single(byAgent.Items);

        var other = await svc.ListAsync(1, 20, new AgentQuoteRequestListFilter(AgentId: Guid.NewGuid()));
        Assert.Empty(other.Items);
    }
}
