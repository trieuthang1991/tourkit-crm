using TourKit.Application.Common;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Finance;

/// <summary>
/// Test state machine duyệt phiếu thu NHIỀU CẤP (<see cref="ReceiptApprovalService"/>) qua fake
/// <see cref="IRepository{T}"/> in-memory — nhanh, KHÔNG EF, KHÔNG HTTP.
/// </summary>
public class ReceiptApprovalServiceTests
{
    private static ReceiptApprovalService NewService(
        out FakeRepository<ReceiptVoucher> receiptRepo,
        out FakeRepository<ReceiptApproval> approvalRepo,
        out FakeRepository<ReceiptApprovalStepUser> stepUserRepo)
    {
        receiptRepo = new FakeRepository<ReceiptVoucher>();
        approvalRepo = new FakeRepository<ReceiptApproval>();
        stepUserRepo = new FakeRepository<ReceiptApprovalStepUser>();
        return new ReceiptApprovalService(receiptRepo, approvalRepo, stepUserRepo);
    }

    private static async Task<ReceiptVoucher> SeedReceiptAsync(FakeRepository<ReceiptVoucher> receiptRepo, decimal amount = 5_000_000m)
    {
        var receipt = new ReceiptVoucher
        {
            Code = "RCP-APR",
            Title = "Phiếu thu",
            IssuedAt = DateTimeOffset.UtcNow,
            OrderId = Guid.NewGuid(),
            Amount = amount,
            PaymentMethod = "cash",
            Status = 0,
            IsRecognized = false,
        };
        await receiptRepo.AddAsync(receipt);
        await receiptRepo.SaveChangesAsync();
        return receipt;
    }

    [Fact]
    public async Task StartAsync_creates_approval_InProgress_at_min_step_order()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var approval = await service.StartAsync(receipt.Id, new StartApprovalDto(ApprovalMethod.One,
        [
            new ApprovalStepDto(1, [user1]),
            new ApprovalStepDto(2, [user2]),
        ]));

        Assert.Equal(ApprovalStatus.InProgress, approval.Status);
        Assert.Equal(1, approval.CurrentStepOrder);
        Assert.Equal(2, approval.Steps.Length);
    }

    [Fact]
    public async Task StartAsync_unknown_receipt_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.StartAsync(
            Guid.NewGuid(), new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [Guid.NewGuid()])])));
    }

    [Fact]
    public async Task StartAsync_twice_for_same_receipt_throws_ConflictException()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var body = new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [Guid.NewGuid()])]);
        await service.StartAsync(receipt.Id, body);

        await Assert.ThrowsAsync<ConflictException>(() => service.StartAsync(receipt.Id, body));
    }

    [Fact]
    public async Task StartAsync_no_steps_throws_ValidationAppException()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.StartAsync(
            receipt.Id, new StartApprovalDto(ApprovalMethod.One, [])));
    }

    [Fact]
    public async Task StartAsync_step_without_users_throws_ValidationAppException()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.StartAsync(
            receipt.Id, new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [])])));
    }

    [Fact]
    public async Task ActAsync_MethodOne_two_steps_advances_then_approves_and_recognizes_receipt()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        await service.StartAsync(receipt.Id, new StartApprovalDto(ApprovalMethod.One,
        [
            new ApprovalStepDto(1, [user1]),
            new ApprovalStepDto(2, [user2]),
        ]));

        var afterStep1 = await service.ActAsync(receipt.Id, user1, new ActApprovalDto(true, "ok bước 1"));
        Assert.Equal(ApprovalStatus.InProgress, afterStep1.Status);
        Assert.Equal(2, afterStep1.CurrentStepOrder);

        var stored1 = await receiptRepo.GetByIdAsync(receipt.Id);
        Assert.False(stored1!.IsRecognized);

        var afterStep2 = await service.ActAsync(receipt.Id, user2, new ActApprovalDto(true, "ok bước 2"));
        Assert.Equal(ApprovalStatus.Approved, afterStep2.Status);

        var stored2 = await receiptRepo.GetByIdAsync(receipt.Id);
        Assert.True(stored2!.IsRecognized);
        Assert.Equal(1, stored2.Status);
    }

    [Fact]
    public async Task ActAsync_MethodAll_requires_every_user_at_step_to_approve()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        await service.StartAsync(receipt.Id, new StartApprovalDto(ApprovalMethod.All,
        [
            new ApprovalStepDto(1, [user1, user2]),
        ]));

        var afterFirstUser = await service.ActAsync(receipt.Id, user1, new ActApprovalDto(true, null));
        Assert.Equal(ApprovalStatus.InProgress, afterFirstUser.Status);
        Assert.Equal(1, afterFirstUser.CurrentStepOrder);

        var afterSecondUser = await service.ActAsync(receipt.Id, user2, new ActApprovalDto(true, null));
        Assert.Equal(ApprovalStatus.Approved, afterSecondUser.Status);

        var stored = await receiptRepo.GetByIdAsync(receipt.Id);
        Assert.True(stored!.IsRecognized);
    }

    [Fact]
    public async Task ActAsync_reject_at_first_step_terminates_without_recognizing_receipt()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        await service.StartAsync(receipt.Id, new StartApprovalDto(ApprovalMethod.One,
        [
            new ApprovalStepDto(1, [user1]),
            new ApprovalStepDto(2, [user2]),
        ]));

        var result = await service.ActAsync(receipt.Id, user1, new ActApprovalDto(false, "sai số tiền"));

        Assert.Equal(ApprovalStatus.Rejected, result.Status);
        var stored = await receiptRepo.GetByIdAsync(receipt.Id);
        Assert.False(stored!.IsRecognized);
    }

    [Fact]
    public async Task ActAsync_reject_at_non_first_step_rolls_back_to_previous_step_pending()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        await service.StartAsync(receipt.Id, new StartApprovalDto(ApprovalMethod.One,
        [
            new ApprovalStepDto(1, [user1]),
            new ApprovalStepDto(2, [user2]),
        ]));
        await service.ActAsync(receipt.Id, user1, new ActApprovalDto(true, null)); // → step 2

        var result = await service.ActAsync(receipt.Id, user2, new ActApprovalDto(false, "cần xem lại"));

        Assert.Equal(ApprovalStatus.InProgress, result.Status);
        Assert.Equal(1, result.CurrentStepOrder);
        var step1User = result.Steps.Single(s => s.StepOrder == 1);
        Assert.Equal(StepUserStatus.Pending, step1User.Status);
        Assert.Null(step1User.ActedAt);
    }

    [Fact]
    public async Task ActAsync_wrong_user_for_current_step_throws_ForbiddenException()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var user1 = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        await service.StartAsync(receipt.Id, new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [user1])]));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ActAsync(receipt.Id, stranger, new ActApprovalDto(true, null)));
    }

    [Fact]
    public async Task ActAsync_no_approval_started_throws_NotFoundException()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ActAsync(receipt.Id, Guid.NewGuid(), new ActApprovalDto(true, null)));
    }

    [Fact]
    public async Task ActAsync_after_terminal_status_throws_ConflictException()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var user1 = Guid.NewGuid();
        await service.StartAsync(receipt.Id, new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [user1])]));
        await service.ActAsync(receipt.Id, user1, new ActApprovalDto(false, null)); // → Rejected (terminal)

        await Assert.ThrowsAsync<ConflictException>(() => service.ActAsync(receipt.Id, user1, new ActApprovalDto(true, null)));
    }

    [Fact]
    public async Task GetAsync_returns_current_state()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);
        var user1 = Guid.NewGuid();
        await service.StartAsync(receipt.Id, new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [user1])]));

        var approval = await service.GetAsync(receipt.Id);

        Assert.Equal(ApprovalStatus.InProgress, approval.Status);
    }

    [Fact]
    public async Task GetAsync_no_approval_started_throws_NotFoundException()
    {
        var service = NewService(out var receiptRepo, out _, out _);
        var receipt = await SeedReceiptAsync(receiptRepo);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(receipt.Id));
    }
}
