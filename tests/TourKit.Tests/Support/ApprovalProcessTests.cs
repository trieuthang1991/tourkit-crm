using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Finance;
using TourKit.Shared.Enums;

namespace TourKit.Tests.Support;

/// <summary>Quy trình duyệt cấu hình được (legacy ApprovalProcess) qua /api/v1/approval-processes.</summary>
public class ApprovalProcessTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public ApprovalProcessTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Create_process_add_step_by_position_and_read_detail()
    {
        var client = await LoggedInClientAsync("approval-a");

        var process = await (await client.PostAsJsonAsync("/api/v1/approval-processes",
            new CreateApprovalProcessDto("Duyệt chi trên 10 triệu", (int)ApprovalMethod.All)))
            .Content.ReadFromJsonAsync<ApprovalProcessDto>();
        Assert.Equal(0, process!.StepCount);

        var pos = await (await client.PostAsJsonAsync("/api/v1/positions",
            new CreatePositionDto("Kế toán trưởng", 1))).Content.ReadFromJsonAsync<PositionDto>();

        var step = await (await client.PostAsJsonAsync($"/api/v1/approval-processes/{process.Id}/steps",
            new AddStepDto(pos!.Id))).Content.ReadFromJsonAsync<ApprovalProcessStepDto>();
        Assert.Equal(1, step!.StepOrder);

        var detail = await client.GetFromJsonAsync<ApprovalProcessDetailDto>($"/api/v1/approval-processes/{process.Id}");
        Assert.Single(detail!.Steps);
        Assert.Equal("Kế toán trưởng", detail.Steps[0].PositionName);

        var del = await client.DeleteAsync($"/api/v1/approval-processes/{process.Id}/steps/{step.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var after = await client.GetFromJsonAsync<ApprovalProcessDetailDto>($"/api/v1/approval-processes/{process.Id}");
        Assert.Empty(after!.Steps);
    }
}
