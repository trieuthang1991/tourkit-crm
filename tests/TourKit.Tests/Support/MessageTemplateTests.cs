using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Tests.Support;

/// <summary>Mẫu tin nhắn tái sử dụng (legacy Email_Sample/Marketing_Template) qua /api/v1/message-templates.</summary>
public class MessageTemplateTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public MessageTemplateTests(AuthTestFactory factory) => _factory = factory;

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
    public async Task Create_list_by_channel_and_delete()
    {
        var client = await LoggedInClientAsync("msg-tpl-a");

        (await client.PostAsJsonAsync("/api/v1/message-templates",
            new CreateMessageTemplateDto("Nhắc hạn cọc", MarketingChannel.Sms, null, "Quý khách vui lòng đặt cọc..."))).EnsureSuccessStatusCode();
        var created = await (await client.PostAsJsonAsync("/api/v1/message-templates",
            new CreateMessageTemplateDto("Chào mừng", MarketingChannel.Email, "Xin chào", "Cảm ơn quý khách...")))
            .Content.ReadFromJsonAsync<MessageTemplateDto>();

        var emailOnly = await client.GetFromJsonAsync<List<MessageTemplateDto>>("/api/v1/message-templates?channel=1");
        Assert.Single(emailOnly!);
        Assert.Equal("Chào mừng", emailOnly![0].Name);

        var all = await client.GetFromJsonAsync<List<MessageTemplateDto>>("/api/v1/message-templates");
        Assert.Equal(2, all!.Count);

        var del = await client.DeleteAsync($"/api/v1/message-templates/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }
}
