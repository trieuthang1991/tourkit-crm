using TourKit.Shared.Entities;

namespace TourKit.Api.Finance;

public sealed record ApprovalStep(int StepOrder, Guid[] UserIds);

public sealed record StartApprovalRequest(ApprovalMethod Method, ApprovalStep[] Steps);

public sealed record ActRequest(bool Approve, string? Note);

public sealed record ApprovalStepUserResponse(
    int StepOrder, Guid UserId, StepUserStatus Status, DateTimeOffset? ActedAt, string? Note);

public sealed record ApprovalResponse(
    Guid Id, Guid ReceiptVoucherId, ApprovalMethod Method, int CurrentStepOrder, ApprovalStatus Status,
    ApprovalStepUserResponse[] Steps);
