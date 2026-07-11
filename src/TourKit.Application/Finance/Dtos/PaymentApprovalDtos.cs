using TourKit.Shared.Enums;

namespace TourKit.Application.Finance.Dtos;

/// <summary>
/// DTO luồng duyệt phiếu chi (approval + toàn bộ step users) trả ra cho client.
/// Tái dùng <see cref="StartApprovalDto"/>/<see cref="ActApprovalDto"/>/<see cref="ApprovalStepUserDto"/> cho input.
/// </summary>
public sealed record PaymentApprovalDto(
    Guid Id, Guid PaymentVoucherId, ApprovalMethod Method, int CurrentStepOrder, ApprovalStatus Status,
    ApprovalStepUserDto[] Steps);
