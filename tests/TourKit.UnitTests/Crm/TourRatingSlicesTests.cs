using Microsoft.EntityFrameworkCore;
using TourKit.Api.Crm.Features;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Crm;

/// <summary>
/// Test slice TourRating trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CommissionRuleSlicesTests</c>).
/// </summary>
public class TourRatingSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateTourRatingCommand Valid() =>
        new(Guid.NewGuid(), null, "Nguyễn Văn A", "0900000000", 5, "Rất hài lòng", 0);

    [Fact]
    public void CreateValidator_rejects_stars_out_of_range()
    {
        var v = new CreateTourRatingValidator();

        Assert.False(v.Validate(Valid() with { Stars = 0 }).IsValid);
        Assert.False(v.Validate(Valid() with { Stars = 6 }).IsValid);
        Assert.True(v.Validate(Valid() with { Stars = 1 }).IsValid);
        Assert.True(v.Validate(Valid() with { Stars = 5 }).IsValid);
    }

    [Fact]
    public void UpdateValidator_rejects_stars_out_of_range()
    {
        var v = new UpdateTourRatingValidator();

        Assert.False(v.Validate(new UpdateTourRatingCommand(Guid.NewGuid(), "A", null, 0, null, 0)).IsValid);
        Assert.False(v.Validate(new UpdateTourRatingCommand(Guid.NewGuid(), "A", null, 6, null, 0)).IsValid);
        Assert.True(v.Validate(new UpdateTourRatingCommand(Guid.NewGuid(), "A", null, 3, null, 0)).IsValid);
    }

    [Fact]
    public async Task Create_then_update_then_delete_roundtrip()
    {
        var db = NewDb(new FixedTenant());
        var tourDepartureId = Guid.NewGuid();

        var createHandler = new CreateTourRatingHandler(db);
        var createResult = await createHandler.Handle(
            new CreateTourRatingCommand(tourDepartureId, null, "Nguyễn Văn A", "0900000000", 5, "Rất hài lòng", 0),
            CancellationToken.None);
        Assert.True(createResult.IsSuccess);
        Assert.Equal(tourDepartureId, createResult.Value.TourDepartureId);
        Assert.Equal(5, createResult.Value.Stars);
        var id = createResult.Value.Id;

        var updateHandler = new UpdateTourRatingHandler(db);
        var updateResult = await updateHandler.Handle(
            new UpdateTourRatingCommand(id, "Trần Thị B", "0911111111", 3, "Tạm ổn", 1), CancellationToken.None);
        Assert.True(updateResult.IsSuccess);
        Assert.True(updateResult.Value);

        var listHandler = new ListTourRatingsHandler(db);
        var listResult = await listHandler.Handle(new ListTourRatingsQuery(1, 20), CancellationToken.None);
        Assert.True(listResult.IsSuccess);
        var updated = Assert.Single(listResult.Value.Items);
        Assert.Equal("Trần Thị B", updated.CustomerName);
        Assert.Equal(3, updated.Stars);
        Assert.Equal(1, updated.Status);

        var deleteHandler = new DeleteTourRatingHandler(db);
        var deleteResult = await deleteHandler.Handle(new DeleteTourRatingCommand(id), CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var afterDelete = await listHandler.Handle(new ListTourRatingsQuery(1, 20), CancellationToken.None);
        Assert.Empty(afterDelete.Value.Items);
    }

    [Fact]
    public async Task UpdateTourRatingHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new UpdateTourRatingHandler(db);

        var result = await handler.Handle(
            new UpdateTourRatingCommand(Guid.NewGuid(), "A", null, 5, null, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
