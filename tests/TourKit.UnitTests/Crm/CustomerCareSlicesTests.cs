using Microsoft.EntityFrameworkCore;
using TourKit.Api.Crm.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Crm;

/// <summary>
/// Test slice CustomerCare trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CommissionRuleSlicesTests</c>).
/// </summary>
public class CustomerCareSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static async Task<Guid> SeedCustomer(AppDbContext db)
    {
        var customer = new Customer { FullName = "Nguyễn Văn A" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(CancellationToken.None);
        return customer.Id;
    }

    [Fact]
    public void CreateValidator_rejects_empty_title()
    {
        var v = new CreateCustomerCareValidator();

        Assert.False(v.Validate(new CreateCustomerCareCommand(Guid.NewGuid(), string.Empty, null, null, null, 0)).IsValid);
        Assert.True(v.Validate(new CreateCustomerCareCommand(Guid.NewGuid(), "Gọi nhắc lịch", null, null, null, 0)).IsValid);
    }

    [Fact]
    public void UpdateValidator_rejects_empty_title()
    {
        var v = new UpdateCustomerCareValidator();

        Assert.False(v.Validate(new UpdateCustomerCareCommand(Guid.NewGuid(), string.Empty, null, null, null, null, 0)).IsValid);
        Assert.True(v.Validate(new UpdateCustomerCareCommand(Guid.NewGuid(), "Gọi nhắc lịch", null, null, null, null, 0)).IsValid);
    }

    [Fact]
    public async Task CreateHandler_returns_Validation_when_customer_not_found()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateCustomerCareHandler(db);

        var result = await handler.Handle(
            new CreateCustomerCareCommand(Guid.NewGuid(), "Gọi nhắc lịch", null, null, null, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Create_then_update_then_delete_roundtrip()
    {
        var db = NewDb(new FixedTenant());
        var customerId = await SeedCustomer(db);
        var remindAt = DateTimeOffset.UtcNow.AddDays(3);

        var createHandler = new CreateCustomerCareHandler(db);
        var createResult = await createHandler.Handle(
            new CreateCustomerCareCommand(customerId, "Gọi nhắc lịch", "Chi tiết", remindAt, null, 0), CancellationToken.None);
        Assert.True(createResult.IsSuccess);
        Assert.Equal(customerId, createResult.Value.CustomerId);
        Assert.Equal("Gọi nhắc lịch", createResult.Value.Title);
        var id = createResult.Value.Id;

        var updateHandler = new UpdateCustomerCareHandler(db);
        var updateResult = await updateHandler.Handle(
            new UpdateCustomerCareCommand(id, "Đã gọi", "Đã tư vấn", remindAt, "Khách hài lòng", null, 1), CancellationToken.None);
        Assert.True(updateResult.IsSuccess);
        Assert.True(updateResult.Value);

        var listHandler = new ListCustomerCaresHandler(db);
        var listResult = await listHandler.Handle(new ListCustomerCaresQuery(1, 20), CancellationToken.None);
        Assert.True(listResult.IsSuccess);
        var updated = Assert.Single(listResult.Value.Items);
        Assert.Equal("Đã gọi", updated.Title);
        Assert.Equal("Khách hài lòng", updated.Feedback);
        Assert.Equal(1, updated.Status);

        var deleteHandler = new DeleteCustomerCareHandler(db);
        var deleteResult = await deleteHandler.Handle(new DeleteCustomerCareCommand(id), CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var afterDelete = await listHandler.Handle(new ListCustomerCaresQuery(1, 20), CancellationToken.None);
        Assert.Empty(afterDelete.Value.Items);
    }

    [Fact]
    public async Task UpdateCustomerCareHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new UpdateCustomerCareHandler(db);

        var result = await handler.Handle(
            new UpdateCustomerCareCommand(Guid.NewGuid(), "Gọi nhắc lịch", null, null, null, null, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
