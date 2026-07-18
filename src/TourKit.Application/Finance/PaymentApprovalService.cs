using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Finance;

/// <summary>
/// Duyệt phiếu chi NHIỀU CẤP (đối xứng <see cref="ReceiptApprovalService"/>) — ALTERNATIVE cho duyệt 1 cấp.
/// Khi luồng đạt Approved ở bước cuối, set voucher IsRecognized=true + Status=1 (cùng hiệu ứng duyệt 1 cấp).
/// Method.One: 1 người duyệt là bước qua; Method.All: voting cả bước (chỉ từ chối khi bước bỏ phiếu hết mà
/// có K.duyệt). Từ chối Method.One lùi 1 bước (hoặc kết thúc ở bước đầu).
///
/// Nguồn dựng luồng (<see cref="StartAsync"/>): SNAPSHOT từ template <see cref="ApprovalProcess"/> khi client
/// truyền <c>ApprovalProcessId</c>; nếu không thì fallback theo <c>dto.Steps</c> (tương thích cũ).
/// </summary>
public sealed class PaymentApprovalService(
    IRepository<PaymentVoucher> paymentRepo,
    IRepository<PaymentApproval> approvalRepo,
    IRepository<PaymentApprovalStepUser> stepUserRepo,
    IRepository<ApprovalProcess>? processRepo = null,
    IRepository<ApprovalProcessStep>? processStepRepo = null,
    IRepository<ApprovalProcessStepUser>? processStepUserRepo = null,
    IRepository<User>? userRepo = null,
    INotificationService? notifications = null,
    IEmailSender? email = null) : IPaymentApprovalService
{
    public async Task<PaymentApprovalDto> StartAsync(Guid paymentId, StartApprovalDto dto)
    {
        var payment = await paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
        {
            throw new NotFoundException();
        }

        if (await approvalRepo.AnyAsync(a => a.PaymentVoucherId == paymentId))
        {
            throw new ConflictException("Đã có luồng duyệt cho phiếu chi này.");
        }

        var (method, steps, approvalProcessId) = await ResolveChainAsync(dto);

        var approval = new PaymentApproval
        {
            PaymentVoucherId = paymentId,
            ApprovalProcessId = approvalProcessId,
            Method = method,
            CurrentStepOrder = steps.Min(s => s.StepOrder),
            Status = ApprovalStatus.InProgress,
        };
        await approvalRepo.AddAsync(approval);

        foreach (var step in steps)
        {
            foreach (var userId in step.UserIds)
            {
                await stepUserRepo.AddAsync(new PaymentApprovalStepUser
                {
                    PaymentApprovalId = approval.Id,
                    PaymentVoucherId = paymentId,
                    StepOrder = step.StepOrder,
                    UserId = userId,
                    Status = StepUserStatus.Pending,
                });
            }
        }

        await approvalRepo.SaveChangesAsync();
        await stepUserRepo.SaveChangesAsync();

        return await MapAsync(approval);
    }

    public async Task<PaymentApprovalDto> ActAsync(Guid paymentId, Guid userId, ActApprovalDto dto)
    {
        var approval = FirstOrDefault(await approvalRepo.ListAsync(a => a.PaymentVoucherId == paymentId));
        if (approval is null)
        {
            throw new NotFoundException();
        }

        if (approval.Status != ApprovalStatus.InProgress)
        {
            throw new ConflictException("Luồng duyệt đã kết thúc.");
        }

        var stepUsers = await stepUserRepo.ListAsync(su => su.PaymentApprovalId == approval.Id);

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

        var transition = ApplyTransition(approval, stepUsers, dto.Approve, stepUserRepo.Update);

        if (transition.Recognize)
        {
            var payment = await paymentRepo.GetByIdAsync(paymentId) ?? throw new NotFoundException();
            payment.IsRecognized = true;
            payment.Status = 1;
            paymentRepo.Update(payment);
            await paymentRepo.SaveChangesAsync();
        }

        approvalRepo.Update(approval);
        await approvalRepo.SaveChangesAsync();
        await stepUserRepo.SaveChangesAsync();

        await NotifyTransitionAsync(paymentId, transition, stepUsers);

        return await MapAsync(approval);
    }

    public async Task<PaymentApprovalDto> GetAsync(Guid paymentId)
    {
        var approval = FirstOrDefault(await approvalRepo.ListAsync(a => a.PaymentVoucherId == paymentId));
        if (approval is null)
        {
            throw new NotFoundException();
        }

        return await MapAsync(approval);
    }

    private async Task<(ApprovalMethod Method, IReadOnlyList<ApprovalStepDto> Steps, Guid? ApprovalProcessId)>
        ResolveChainAsync(StartApprovalDto dto)
    {
        if (dto.ApprovalProcessId is { } processId)
        {
            if (processRepo is null || processStepRepo is null || processStepUserRepo is null)
            {
                throw new ValidationAppException("Không hỗ trợ dựng luồng từ quy trình duyệt trong ngữ cảnh này.");
            }

            var process = await processRepo.GetByIdAsync(processId)
                ?? throw new ValidationAppException("Quy trình duyệt không tồn tại.");
            if (process.Status != 0)
            {
                throw new ValidationAppException("Quy trình duyệt đã ngừng sử dụng.");
            }

            var defSteps = (await processStepRepo.ListAsync(s => s.ApprovalProcessId == processId))
                .OrderBy(s => s.StepOrder).ToList();
            if (defSteps.Count == 0)
            {
                throw new ValidationAppException("Quy trình duyệt chưa có bước nào.");
            }

            var stepIds = defSteps.Select(s => s.Id).ToHashSet();
            var defUsers = (await processStepUserRepo.ListAsync()).Where(u => stepIds.Contains(u.ApprovalProcessStepId)).ToList();

            var steps = defSteps.Select(s => new ApprovalStepDto(
                s.StepOrder,
                defUsers.Where(u => u.ApprovalProcessStepId == s.Id).Select(u => u.UserId).ToArray())).ToList();

            if (steps.Any(s => s.UserIds.Length == 0))
            {
                throw new ValidationAppException("Mỗi bước của quy trình duyệt phải có ít nhất 1 người duyệt.");
            }

            return (process.Method, steps, processId);
        }

        if (dto.Steps.Length == 0 || dto.Steps.Any(s => s.UserIds.Length == 0))
        {
            throw new ValidationAppException("Phải có ít nhất 1 bước, mỗi bước phải có ít nhất 1 người duyệt.");
        }

        return (dto.Method, dto.Steps, null);
    }

    private static T? FirstOrDefault<T>(IReadOnlyList<T> items) where T : class => items.Count > 0 ? items[0] : null;

    private static ApprovalTransition ApplyTransition(
        PaymentApproval approval,
        IReadOnlyList<PaymentApprovalStepUser> stepUsers,
        bool approve,
        Action<PaymentApprovalStepUser> update)
    {
        var stepOrders = stepUsers.Select(su => su.StepOrder).Distinct().OrderBy(o => o).ToList();
        var firstStepOrder = stepOrders[0];
        var lastStepOrder = stepOrders[^1];
        var currentStep = approval.CurrentStepOrder;
        var atStep = stepUsers.Where(su => su.StepOrder == currentStep).ToList();

        if (approve)
        {
            var stepComplete = approval.Method == ApprovalMethod.One
                || atStep.All(su => su.Status != StepUserStatus.Pending);
            if (!stepComplete)
            {
                return ApprovalTransition.None; // chờ những người còn lại của bước bỏ phiếu
            }

            if (approval.Method == ApprovalMethod.All && atStep.Any(su => su.Status == StepUserStatus.Rejected))
            {
                approval.Status = ApprovalStatus.Rejected;
                return ApprovalTransition.RejectedTerminal(PreviousApprovers(stepUsers, stepOrders, currentStep));
            }

            if (currentStep == lastStepOrder)
            {
                approval.Status = ApprovalStatus.Approved;
                return ApprovalTransition.Approved();
            }

            var nextStepOrder = stepOrders.First(o => o > currentStep);
            approval.CurrentStepOrder = nextStepOrder;
            var nextApprovers = stepUsers
                .Where(su => su.StepOrder == nextStepOrder && su.Status == StepUserStatus.Pending)
                .Select(su => su.UserId).ToList();
            return ApprovalTransition.Advanced(nextStepOrder, nextApprovers);
        }

        if (approval.Method == ApprovalMethod.All)
        {
            var stillPending = atStep.Any(su => su.Status == StepUserStatus.Pending);
            if (!stillPending)
            {
                approval.Status = ApprovalStatus.Rejected;
                return ApprovalTransition.RejectedTerminal(PreviousApprovers(stepUsers, stepOrders, currentStep));
            }

            return ApprovalTransition.None; // voting còn dở → giữ InProgress
        }

        if (currentStep == firstStepOrder)
        {
            approval.Status = ApprovalStatus.Rejected;
            return ApprovalTransition.RejectedTerminal([]);
        }

        var previousStepOrder = stepOrders.Last(o => o < currentStep);
        approval.CurrentStepOrder = previousStepOrder;
        var prevApprovers = new List<Guid>();
        foreach (var su in stepUsers.Where(su => su.StepOrder == previousStepOrder))
        {
            prevApprovers.Add(su.UserId);
            su.Status = StepUserStatus.Pending;
            su.ActedAt = null;
            update(su);
        }

        return ApprovalTransition.Rewound(previousStepOrder, prevApprovers);
    }

    private static List<Guid> PreviousApprovers(
        IReadOnlyList<PaymentApprovalStepUser> stepUsers, IReadOnlyList<int> stepOrders, int currentStep)
    {
        var prev = stepOrders.Where(o => o < currentStep).Cast<int?>().LastOrDefault();
        if (prev is null)
        {
            return [];
        }

        return stepUsers
            .Where(su => su.StepOrder == prev.Value && su.Status == StepUserStatus.Approved)
            .Select(su => su.UserId).ToList();
    }

    private async Task NotifyTransitionAsync(
        Guid paymentId, ApprovalTransition transition, IReadOnlyList<PaymentApprovalStepUser> stepUsers)
    {
        if (transition.Kind == TransitionKind.None || transition.Kind == TransitionKind.Approved)
        {
            // Approved: có thể thông báo người tạo phiếu — hiện chưa có trường người tạo trên voucher nên bỏ qua.
            return;
        }

        if (transition.Targets.Count == 0)
        {
            return;
        }

        var payment = await paymentRepo.GetByIdAsync(paymentId);
        var code = payment?.Code ?? "";
        var (title, message) = transition.Kind switch
        {
            TransitionKind.Advanced =>
                ($"Phiếu {code} chờ bạn duyệt (bước {transition.StepOrder})",
                 $"Phiếu chi {code} đã chuyển sang bước {transition.StepOrder}, cần bạn duyệt."),
            _ => // Rewound / RejectedTerminal
                ($"Phiếu {code} bị từ chối, cần xử lý lại",
                 $"Phiếu chi {code} bị từ chối, cần bước trước xử lý lại."),
        };

        await NotifyAsync(transition.Targets, title, message);
    }

    private async Task NotifyAsync(IReadOnlyList<Guid> userIds, string title, string message)
    {
        const string linkUrl = "/payments";

        if (notifications is not null)
        {
            foreach (var uid in userIds)
            {
                try
                {
                    await notifications.PushAsync(uid, title, message, linkUrl, "approval");
                }
                catch
                {
                    // nuốt lỗi — thông báo không được chặn luồng duyệt
                }
            }
        }

        if (email is not null && userRepo is not null)
        {
            var users = await userRepo.ListAsync(u => userIds.Contains(u.Id));
            foreach (var u in users.Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email)))
            {
                try
                {
                    await email.SendAsync(u.Email, title, message);
                }
                catch
                {
                    // best-effort
                }
            }
        }
    }

    private async Task<PaymentApprovalDto> MapAsync(PaymentApproval approval)
    {
        var stepUsers = await stepUserRepo.ListAsync(su => su.PaymentApprovalId == approval.Id);
        var steps = stepUsers.OrderBy(su => su.StepOrder).ThenBy(su => su.UserId)
            .Select(su => new ApprovalStepUserDto(su.StepOrder, su.UserId, su.Status, su.ActedAt, su.Note))
            .ToArray();

        return new PaymentApprovalDto(
            approval.Id, approval.PaymentVoucherId, approval.Method, approval.CurrentStepOrder, approval.Status,
            steps, approval.ApprovalProcessId);
    }
}
