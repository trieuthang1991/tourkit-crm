using TourKit.Application.Common;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Finance;

/// <summary>
/// State machine duyệt phiếu chi NHIỀU CẤP (<see cref="PaymentApprovalService"/>) qua fake
/// <see cref="IRepository{T}"/> in-memory — đối xứng ReceiptApprovalServiceTests.
/// </summary>
public class PaymentApprovalServiceTests
{
    private static PaymentApprovalService NewService(
        out FakeRepository<PaymentVoucher> paymentRepo,
        out FakeRepository<PaymentApproval> approvalRepo,
        out FakeRepository<PaymentApprovalStepUser> stepUserRepo)
    {
        paymentRepo = new FakeRepository<PaymentVoucher>();
        approvalRepo = new FakeRepository<PaymentApproval>();
        stepUserRepo = new FakeRepository<PaymentApprovalStepUser>();
        return new PaymentApprovalService(paymentRepo, approvalRepo, stepUserRepo);
    }

    private static async Task<PaymentVoucher> SeedPaymentAsync(FakeRepository<PaymentVoucher> repo)
    {
        var payment = new PaymentVoucher
        {
            Code = "PAY-APR",
            Title = "Phiếu chi",
            IssuedAt = DateTimeOffset.UtcNow,
            OrderId = Guid.NewGuid(),
            Amount = 3_000_000m,
            PaymentMethod = "cash",
            Status = 0,
            IsRecognized = false,
        };
        await repo.AddAsync(payment);
        await repo.SaveChangesAsync();
        return payment;
    }

    [Fact]
    public async Task StartAsync_creates_approval_InProgress_at_min_step_order()
    {
        var service = NewService(out var paymentRepo, out _, out _);
        var payment = await SeedPaymentAsync(paymentRepo);

        var approval = await service.StartAsync(payment.Id, new StartApprovalDto(ApprovalMethod.One,
        [
            new ApprovalStepDto(1, [Guid.NewGuid()]),
            new ApprovalStepDto(2, [Guid.NewGuid()]),
        ]));

        Assert.Equal(ApprovalStatus.InProgress, approval.Status);
        Assert.Equal(1, approval.CurrentStepOrder);
        Assert.Equal(2, approval.Steps.Length);
    }

    [Fact]
    public async Task StartAsync_unknown_payment_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.StartAsync(
            Guid.NewGuid(), new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [Guid.NewGuid()])])));
    }

    [Fact]
    public async Task StartAsync_twice_throws_ConflictException()
    {
        var service = NewService(out var paymentRepo, out _, out _);
        var payment = await SeedPaymentAsync(paymentRepo);
        var body = new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [Guid.NewGuid()])]);
        await service.StartAsync(payment.Id, body);

        await Assert.ThrowsAsync<ConflictException>(() => service.StartAsync(payment.Id, body));
    }

    [Fact]
    public async Task Approve_last_step_recognizes_payment()
    {
        var service = NewService(out var paymentRepo, out _, out _);
        var payment = await SeedPaymentAsync(paymentRepo);
        var user = Guid.NewGuid();
        await service.StartAsync(payment.Id, new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [user])]));

        var result = await service.ActAsync(payment.Id, user, new ActApprovalDto(true, "ok"));

        Assert.Equal(ApprovalStatus.Approved, result.Status);
        var updated = await paymentRepo.GetByIdAsync(payment.Id);
        Assert.True(updated!.IsRecognized);
        Assert.Equal(1, updated.Status);
    }

    [Fact]
    public async Task Act_by_non_assignee_throws_ForbiddenException()
    {
        var service = NewService(out var paymentRepo, out _, out _);
        var payment = await SeedPaymentAsync(paymentRepo);
        await service.StartAsync(payment.Id, new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [Guid.NewGuid()])]));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ActAsync(payment.Id, Guid.NewGuid(), new ActApprovalDto(true, null)));
    }

    [Fact]
    public async Task Reject_at_first_step_rejects_flow_without_recognizing()
    {
        var service = NewService(out var paymentRepo, out _, out _);
        var payment = await SeedPaymentAsync(paymentRepo);
        var user = Guid.NewGuid();
        await service.StartAsync(payment.Id, new StartApprovalDto(ApprovalMethod.One, [new ApprovalStepDto(1, [user])]));

        var result = await service.ActAsync(payment.Id, user, new ActApprovalDto(false, "không duyệt"));

        Assert.Equal(ApprovalStatus.Rejected, result.Status);
        var updated = await paymentRepo.GetByIdAsync(payment.Id);
        Assert.False(updated!.IsRecognized);
    }
}
