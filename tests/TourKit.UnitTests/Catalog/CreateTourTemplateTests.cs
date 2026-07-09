using Microsoft.EntityFrameworkCore;
using TourKit.Api.Catalog.Features;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Catalog;

/// <summary>
/// Minh hoạ lợi ích kiến trúc slice: test HANDLER + VALIDATOR trực tiếp — nhanh, KHÔNG HTTP, KHÔNG server.
/// </summary>
public class CreateTourTemplateTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateTourTemplateCommand Valid() =>
        new("T-1", "Đà Nẵng", "domestic", 30, 24, 5_000_000m, 3_000_000m, 0m, 0m, "ĐK");

    [Fact]
    public void Validator_rejects_empty_code_and_negative_price()
    {
        var v = new CreateTourTemplateValidator();

        Assert.False(v.Validate(Valid() with { Code = "" }).IsValid);
        Assert.False(v.Validate(Valid() with { PriceAdult = -1m }).IsValid);
        Assert.True(v.Validate(Valid()).IsValid);
    }

    [Fact]
    public async Task Handler_creates_template_and_returns_response()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateTourTemplateHandler(db);

        var result = await handler.Handle(Valid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5_000_000m, result.Value.PriceAdult);
        Assert.Equal(1, await db.TourTemplates.CountAsync());
    }

    [Fact]
    public async Task Handler_returns_Conflict_on_duplicate_code()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        db.TourTemplates.Add(new TourTemplate { Code = "T-1", Title = "cũ" });
        await db.SaveChangesAsync();

        var result = await new CreateTourTemplateHandler(db).Handle(Valid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TourKit.Shared.Application.ErrorType.Conflict, result.Error!.Type);
    }
}
