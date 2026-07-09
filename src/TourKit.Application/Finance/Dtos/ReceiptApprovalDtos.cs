using TourKit.Shared.Enums;

namespace TourKit.Application.Finance.Dtos;

/// <summary>Một bước của luồng duyệt nhiều cấp — người duyệt được phân công tại bước này.</summary>
public sealed record ApprovalStepDto(int StepOrder, Guid[] UserIds);

/// <summary>DTO khởi tạo luồng duyệt nhiều cấp cho một phiếu thu.</summary>
public sealed record StartApprovalDto(ApprovalMethod Method, ApprovalStepDto[] Steps);

/// <summary>DTO duyệt/từ chối 1 bước của luồng duyệt nhiều cấp.</summary>
public sealed record ActApprovalDto(bool Approve, string? Note);

/// <summary>Trạng thái một người duyệt tại một bước — trả ra cho client.</summary>
public sealed record ApprovalStepUserDto(
    int StepOrder, Guid UserId, StepUserStatus Status, DateTimeOffset? ActedAt, string? Note);

/// <summary>DTO luồng duyệt (approval + toàn bộ step users) trả ra cho client.</summary>
public sealed record ApprovalDto(
    Guid Id, Guid ReceiptVoucherId, ApprovalMethod Method, int CurrentStepOrder, ApprovalStatus Status,
    ApprovalStepUserDto[] Steps);
