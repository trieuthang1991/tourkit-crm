namespace TourKit.Application.Commission.Dtos;

/// <summary>DTO trả ra cho client (không lộ entity). UserName enrich từ User để hiển thị tên thay vì GUID.</summary>
public sealed record CommissionRuleDto(Guid Id, Guid UserId, decimal Percentage, int Status, string? UserName = null);

/// <summary>Bộ lọc quy tắc hoa hồng: nhân viên · trạng thái · từ khoá (tên nhân viên). Status: 1 áp dụng, 0 tạm ngừng.</summary>
public sealed record CommissionRuleListFilter(string? Q = null, Guid? UserId = null, int? Status = null);

/// <summary>Thẻ thống kê đầu màn Hoa hồng: tổng · đang áp dụng · tạm ngừng · tỉ lệ trung bình.</summary>
public sealed record CommissionRuleStatsDto(int Total, int Active, int Inactive, decimal AvgPercentage);

/// <summary>DTO tạo quy tắc hoa hồng mới cho 1 user.</summary>
public sealed record CreateCommissionRuleDto(Guid UserId, decimal Percentage, int Status);

/// <summary>DTO cập nhật quy tắc hoa hồng.</summary>
public sealed record UpdateCommissionRuleDto(decimal Percentage, int Status);
