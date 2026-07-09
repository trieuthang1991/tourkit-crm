using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Finance;

/// <summary>
/// Duyệt phiếu thu NHIỀU CẤP (legacy ReceiptVoucherApproval + ReceiptVoucherApprovalStepUser) —
/// ALTERNATIVE cho duyệt 1 cấp ở <see cref="IReceiptService"/> (Approve/Reject vẫn giữ nguyên).
/// Khi luồng nhiều cấp đạt Approved ở bước cuối, set voucher IsRecognized=true + Status=1 (cùng hiệu
/// ứng duyệt 1 cấp). Thu hồi (recall) một hành động đã duyệt/từ chối là OUT OF SCOPE — chưa hỗ trợ.
/// </summary>
public sealed class ReceiptApprovalService(
    IRepository<ReceiptVoucher> receiptRepo,
    IRepository<ReceiptApproval> approvalRepo,
    IRepository<ReceiptApprovalStepUser> stepUserRepo) : IReceiptApprovalService
{
    public async Task<ApprovalDto> StartAsync(Guid receiptId, StartApprovalDto dto)
    {
        var receipt = await receiptRepo.GetByIdAsync(receiptId);
        if (receipt is null)
        {
            throw new NotFoundException();
        }

        if (await approvalRepo.AnyAsync(a => a.ReceiptVoucherId == receiptId))
        {
            throw new ConflictException("Đã có luồng duyệt cho phiếu thu này.");
        }

        if (dto.Steps.Length == 0 || dto.Steps.Any(s => s.UserIds.Length == 0))
        {
            throw new ValidationAppException("Phải có ít nhất 1 bước, mỗi bước phải có ít nhất 1 người duyệt.");
        }

        var approval = new ReceiptApproval
        {
            ReceiptVoucherId = receiptId,
            Method = dto.Method,
            CurrentStepOrder = dto.Steps.Min(s => s.StepOrder),
            Status = ApprovalStatus.InProgress,
        };
        await approvalRepo.AddAsync(approval);

        foreach (var step in dto.Steps)
        {
            foreach (var userId in step.UserIds)
            {
                await stepUserRepo.AddAsync(new ReceiptApprovalStepUser
                {
                    ReceiptApprovalId = approval.Id,
                    ReceiptVoucherId = receiptId,
                    StepOrder = step.StepOrder,
                    UserId = userId,
                    Status = StepUserStatus.Pending,
                });
            }
        }

        // Flush riêng từng repo (tương thích FakeRepository trong unit test — mỗi loại lưu riêng).
        await approvalRepo.SaveChangesAsync();
        await stepUserRepo.SaveChangesAsync();

        return await MapAsync(approval);
    }

    public async Task<ApprovalDto> ActAsync(Guid receiptId, Guid userId, ActApprovalDto dto)
    {
        var approval = FirstOrDefault(await approvalRepo.ListAsync(a => a.ReceiptVoucherId == receiptId));
        if (approval is null)
        {
            throw new NotFoundException();
        }

        if (approval.Status != ApprovalStatus.InProgress)
        {
            throw new ConflictException("Luồng duyệt đã kết thúc.");
        }

        var stepUsers = await stepUserRepo.ListAsync(su => su.ReceiptApprovalId == approval.Id);

        var actingStepUser = stepUsers.FirstOrDefault(su =>
            su.StepOrder == approval.CurrentStepOrder &&
            su.UserId == userId &&
            su.Status == StepUserStatus.Pending);
        if (actingStepUser is null)
        {
            throw new ForbiddenException();
        }

        actingStepUser.Status = dto.Approve ? StepUserStatus.Approved : StepUserStatus.Rejected;
        actingStepUser.ActedAt = DateTimeOffset.UtcNow;
        actingStepUser.Note = dto.Note;
        stepUserRepo.Update(actingStepUser);

        var stepOrders = stepUsers.Select(su => su.StepOrder).Distinct().OrderBy(o => o).ToList();
        var firstStepOrder = stepOrders[0];
        var lastStepOrder = stepOrders[^1];

        if (dto.Approve)
        {
            var stepPasses = approval.Method == ApprovalMethod.One ||
                              stepUsers.Where(su => su.StepOrder == approval.CurrentStepOrder)
                                  .All(su => su.Status == StepUserStatus.Approved);

            if (stepPasses)
            {
                if (approval.CurrentStepOrder == lastStepOrder)
                {
                    approval.Status = ApprovalStatus.Approved;

                    var receipt = await receiptRepo.GetByIdAsync(receiptId);
                    if (receipt is null)
                    {
                        throw new NotFoundException();
                    }

                    receipt.IsRecognized = true;
                    receipt.Status = 1;
                    receiptRepo.Update(receipt);
                    await receiptRepo.SaveChangesAsync();
                }
                else
                {
                    approval.CurrentStepOrder = stepOrders.First(o => o > approval.CurrentStepOrder);
                }
            }
        }
        else
        {
            if (approval.Method == ApprovalMethod.All)
            {
                approval.Status = ApprovalStatus.Rejected;
            }
            else if (approval.CurrentStepOrder == firstStepOrder)
            {
                approval.Status = ApprovalStatus.Rejected;
            }
            else
            {
                var previousStepOrder = stepOrders.Last(o => o < approval.CurrentStepOrder);
                approval.CurrentStepOrder = previousStepOrder;
                foreach (var su in stepUsers.Where(su => su.StepOrder == previousStepOrder))
                {
                    su.Status = StepUserStatus.Pending;
                    su.ActedAt = null;
                    stepUserRepo.Update(su);
                }
            }
        }

        approvalRepo.Update(approval);
        await approvalRepo.SaveChangesAsync();
        await stepUserRepo.SaveChangesAsync();

        return await MapAsync(approval);
    }

    public async Task<ApprovalDto> GetAsync(Guid receiptId)
    {
        var approval = FirstOrDefault(await approvalRepo.ListAsync(a => a.ReceiptVoucherId == receiptId));
        if (approval is null)
        {
            throw new NotFoundException();
        }

        return await MapAsync(approval);
    }

    private static T? FirstOrDefault<T>(IReadOnlyList<T> items) where T : class => items.Count > 0 ? items[0] : null;

    private async Task<ApprovalDto> MapAsync(ReceiptApproval approval)
    {
        var stepUsers = await stepUserRepo.ListAsync(su => su.ReceiptApprovalId == approval.Id);
        var steps = stepUsers.OrderBy(su => su.StepOrder).ThenBy(su => su.UserId)
            .Select(su => new ApprovalStepUserDto(su.StepOrder, su.UserId, su.Status, su.ActedAt, su.Note))
            .ToArray();

        return new ApprovalDto(
            approval.Id, approval.ReceiptVoucherId, approval.Method, approval.CurrentStepOrder, approval.Status, steps);
    }
}
