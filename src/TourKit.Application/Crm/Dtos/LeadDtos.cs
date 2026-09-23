using TourKit.Shared.Enums;

namespace TourKit.Application.Crm.Dtos;

/// <summary>
/// Một khách tiềm năng. Ba trường hay bị lẫn:
/// <c>Source</c> là giá trị CHUẨN lấy từ danh mục /nguon-khach (chiều để gộp báo cáo);
/// <c>Attribution</c> là nguồn CHI TIẾT tuỳ ý (utm_*, trang đích, referrer);
/// <c>Note</c> là nhu cầu khách tự nêu, bằng lời của họ.
/// </summary>
public sealed record LeadDto(
    Guid Id, string FullName, string? Phone, string? Email, string? Source,
    LeadStatus Status, Guid? AssignedToUserId, Guid? ConvertedCustomerId, Guid? BranchId = null,
    string? Note = null, LeadAttribution? Attribution = null);

/// <summary>
/// Tạo khách tiềm năng.
///
/// <c>CampaignId</c> CỐ Ý chỉ mở ở DTO/API và KHÔNG có ô chọn trên form CRM: chiến dịch là thứ
/// người dựng form thu lead gán sẵn cho cả đợt, không phải thứ nhân viên bán hàng ngồi chọn cho
/// từng số — một ô không ai điền là một ô mãi mãi rỗng.
/// </summary>
public sealed record CreateLeadDto(
    string FullName, string? Phone, string? Email, string? Source, Guid? AssignedToUserId, Guid? BranchId = null,
    string? Note = null, LeadAttribution? Attribution = null, Guid? CampaignId = null,
    string? CampaignCode = null);

public sealed record UpdateLeadDto(
    string FullName, string? Phone, string? Email, string? Source, LeadStatus Status, Guid? AssignedToUserId,
    Guid? BranchId = null, string? Note = null, LeadAttribution? Attribution = null);

/// <param name="CustomerId">Hồ sơ khách hàng mà lead này giờ trỏ tới.</param>
/// <param name="DaGanVaoKhachSanCo">
/// <c>true</c> = số điện thoại đã thuộc một khách có sẵn nên lead được NỐI vào đó, không tạo hồ sơ
/// mới. Màn hình phải nói rõ điều này: người bấm nút đang đinh ninh mình vừa tạo một khách hàng.
/// </param>
public sealed record ConvertLeadResultDto(Guid CustomerId, bool DaGanVaoKhachSanCo);

/// <summary>Bộ lọc danh sách Lead (bám thanh lọc hệ cũ). Tất cả optional.</summary>
public sealed record LeadListFilter(
    string? Q = null, int? Status = null, string? Source = null, Guid? AssignedToUserId = null,
    DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedTo = null, Guid? BranchId = null,
    Guid? CreatedByUserId = null, Guid? CampaignId = null);

/// <summary>Thẻ thống kê đầu màn Lead: tổng + đếm theo trạng thái + đã chuyển KH.</summary>
public sealed record LeadStatsDto(
    int Total, int New, int Contacted, int Qualified, int Won, int Lost, int Converted);

/// <summary>Giá trị có sẵn cho dropdown lọc Lead (nguồn khách).</summary>
public sealed record LeadFilterOptionsDto(IReadOnlyList<string> Sources);
