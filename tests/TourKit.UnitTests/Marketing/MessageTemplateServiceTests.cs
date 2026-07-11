using TourKit.Application.Common;
using TourKit.Application.Marketing;
using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Marketing;

/// <summary>Test <see cref="MessageTemplateService"/> — mẫu tin nhắn tái sử dụng (legacy Email_Sample/Marketing_Template).</summary>
public class MessageTemplateServiceTests
{
    private static MessageTemplateService NewService(out FakeRepository<MessageTemplate> repo)
    {
        repo = new FakeRepository<MessageTemplate>();
        return new MessageTemplateService(repo);
    }

    [Fact]
    public async Task CreateAsync_persists_template()
    {
        var service = NewService(out _);

        var dto = await service.CreateAsync(
            new CreateMessageTemplateDto("Chào mừng KH mới", MarketingChannel.Email, "Xin chào", "Nội dung"));

        Assert.Equal("Chào mừng KH mới", dto.Name);
        Assert.Equal(MarketingChannel.Email, dto.Channel);
        Assert.Equal("Xin chào", dto.Subject);
    }

    [Fact]
    public async Task CreateAsync_blank_name_or_body_throws()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateMessageTemplateDto(" ", MarketingChannel.Sms, null, "x")));
        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateMessageTemplateDto("T", MarketingChannel.Sms, null, " ")));
    }

    [Fact]
    public async Task ListAsync_filters_by_channel()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateMessageTemplateDto("E", MarketingChannel.Email, null, "b"));
        await service.CreateAsync(new CreateMessageTemplateDto("S", MarketingChannel.Sms, null, "b"));

        Assert.Equal(2, (await service.ListAsync(null)).Count);
        Assert.Single(await service.ListAsync(MarketingChannel.Sms));
    }

    [Fact]
    public async Task UpdateAsync_changes_fields()
    {
        var service = NewService(out _);
        var created = await service.CreateAsync(new CreateMessageTemplateDto("T", MarketingChannel.Email, "s", "b"));

        await service.UpdateAsync(created.Id, new UpdateMessageTemplateDto("T2", MarketingChannel.Zalo, null, "b2"));

        var updated = (await service.ListAsync(null)).Single();
        Assert.Equal("T2", updated.Name);
        Assert.Equal(MarketingChannel.Zalo, updated.Channel);
        Assert.Null(updated.Subject);
    }

    [Fact]
    public async Task DeleteAsync_unknown_throws()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }
}
