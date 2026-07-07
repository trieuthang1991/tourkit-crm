using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Quyền hạt nền tảng (global — KHÔNG thuộc tenant). Seed từ catalog code.</summary>
public sealed class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}
