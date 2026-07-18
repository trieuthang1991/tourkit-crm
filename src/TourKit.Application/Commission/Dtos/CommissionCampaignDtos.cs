namespace TourKit.Application.Commission.Dtos;

/// <summary>Dòng tóm tắt chính sách hoa hồng bậc thang (danh sách): số nhân viên + số bậc kèm theo.</summary>
public sealed record CommissionCampaignDto(
    Guid Id, string Name, DateTimeOffset StartDate, DateTimeOffset EndDate, int Status, int UserCount, int TierCount);

/// <summary>Một bậc lợi nhuận trả ra client.</summary>
public sealed record CommissionTierDto(Guid Id, decimal StartAmount, decimal EndAmount, decimal Percentage);

/// <summary>Chi tiết chính sách: nhân viên áp dụng (kèm tên) + các bậc (sắp theo StartAmount).</summary>
public sealed record CommissionCampaignDetailDto(
    Guid Id, string Name, DateTimeOffset StartDate, DateTimeOffset EndDate, int Status,
    IReadOnlyList<Guid> UserIds, IReadOnlyList<string> UserNames, IReadOnlyList<CommissionTierDto> Tiers);

/// <summary>Bậc đầu vào khi tạo/sửa chính sách (không có Id — thay toàn bộ khi lưu).</summary>
public sealed record CommissionTierInputDto(decimal StartAmount, decimal EndAmount, decimal Percentage);

/// <summary>DTO tạo chính sách hoa hồng bậc thang: header + danh sách nhân viên + danh sách bậc.</summary>
public sealed record CreateCommissionCampaignDto(
    string Name, DateTimeOffset StartDate, DateTimeOffset EndDate, int Status,
    IReadOnlyList<Guid> UserIds, IReadOnlyList<CommissionTierInputDto> Tiers);

/// <summary>DTO cập nhật chính sách (thay toàn bộ nhân viên + bậc — replace-children).</summary>
public sealed record UpdateCommissionCampaignDto(
    string Name, DateTimeOffset StartDate, DateTimeOffset EndDate, int Status,
    IReadOnlyList<Guid> UserIds, IReadOnlyList<CommissionTierInputDto> Tiers);

/// <summary>
/// Kết quả tra cứu tỉ lệ hoa hồng bậc thang cho (nhân viên, ngày, lợi nhuận).
/// <see cref="Found"/>=false nghĩa là không có chính sách áp dụng → gọi nên fallback về CommissionRule phẳng.
/// </summary>
public sealed record TieredRateResultDto(
    bool Found, Guid? CampaignId, string? CampaignName, decimal Rate, decimal Commission);
