namespace TourKit.Application.Finance;

/// <summary>Loại chuyển trạng thái sau 1 hành động duyệt/K.duyệt — quyết định thông báo cho ai.</summary>
internal enum TransitionKind
{
    /// <summary>Không đổi bước (voting còn dở) — không thông báo.</summary>
    None,

    /// <summary>Tiến sang bước kế — thông báo người duyệt của bước mới.</summary>
    Advanced,

    /// <summary>Lùi về bước trước (Method.One K.duyệt) — thông báo người bước trước.</summary>
    Rewound,

    /// <summary>Đã duyệt xong ở bước cuối — ghi nhận voucher.</summary>
    Approved,

    /// <summary>Bị từ chối (kết thúc) — thông báo người bước trước (nếu có).</summary>
    RejectedTerminal,
}

/// <summary>
/// Ý ĐỊNH thông báo + ghi nhận sau 1 transition — tách state machine (thuần, dễ test) khỏi side-effect
/// (email/notification/ghi nhận công nợ). Dùng chung cho luồng duyệt phiếu thu &amp; phiếu chi.
/// </summary>
internal sealed record ApprovalTransition(
    TransitionKind Kind, int StepOrder, IReadOnlyList<Guid> Targets, bool Recognize)
{
    public static readonly ApprovalTransition None = new(TransitionKind.None, 0, [], false);

    public static ApprovalTransition Advanced(int stepOrder, IReadOnlyList<Guid> targets) =>
        new(TransitionKind.Advanced, stepOrder, targets, false);

    public static ApprovalTransition Rewound(int stepOrder, IReadOnlyList<Guid> targets) =>
        new(TransitionKind.Rewound, stepOrder, targets, false);

    public static ApprovalTransition Approved() =>
        new(TransitionKind.Approved, 0, [], true);

    public static ApprovalTransition RejectedTerminal(IReadOnlyList<Guid> targets) =>
        new(TransitionKind.RejectedTerminal, 0, targets, false);
}
