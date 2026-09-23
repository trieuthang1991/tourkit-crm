namespace TourKit.Application.Crm.Dtos;

/// <summary>Chiến dịch chia số Sale — 1 dòng: chiến dịch + tiến độ chăm sóc + tỷ lệ chốt của lead trong chiến dịch.</summary>
public sealed record LeadCampaignDto(
    Guid Id, string Name, Guid? CreatedByUserId, string? CreatedByName, DateTimeOffset CreatedAt, int Status,
    int TotalLeads, int CaredCount, int ClosedCount, decimal Progress, decimal CloseRate,
    string Code = "", int AssignMode = 0, IReadOnlyList<Guid>? Assignees = null);

/// <summary>Bộ lọc chiến dịch chia số: tên chiến dịch · người tạo.</summary>
public sealed record LeadCampaignListFilter(string? Q = null, Guid? CreatedByUserId = null);

/// <summary>Thẻ đầu màn: tổng chiến dịch · tổng leads · tỷ lệ chốt TB · hoàn thành.</summary>
public sealed record LeadCampaignStatsDto(int TotalCampaigns, int TotalLeads, decimal AvgCloseRate, int Completed);

/// <summary>
/// Tạo chiến dịch chia số mới. Mã (<c>CD-2026-007</c>) do hệ thống sinh, không nhận từ ngoài vào —
/// nó là khoá mà form thu lead dùng để tra, nên phải do một nơi duy nhất quyết định.
/// </summary>
public sealed record CreateLeadCampaignDto(
    string Name, string? Note, int AssignMode = 0, IReadOnlyList<Guid>? Assignees = null);

/// <summary>Sửa chiến dịch: tên, ghi chú, và cấu hình chia số.</summary>
public sealed record UpdateLeadCampaignDto(
    string Name, string? Note, int AssignMode = 0, IReadOnlyList<Guid>? Assignees = null);

/// <summary>
/// Kết quả tra chiến dịch theo mã khi một lead đi vào.
/// </summary>
/// <param name="CampaignId">Chiến dịch tra được.</param>
/// <param name="AssignedToUserId">
/// Người được chia. <c>null</c> khi chiến dịch không bật tự chia hoặc nhóm rỗng — lead vẫn vào
/// được, chỉ là chưa ai phụ trách.
/// </param>
public sealed record ChiaSoKetQuaDto(Guid CampaignId, Guid? AssignedToUserId);

/// <summary>
/// Cấu hình chia số của một chiến dịch, tra theo mã.
///
/// Tách riêng khỏi <see cref="ChiaSoKetQuaDto"/> là CÓ CHỦ Ý và là điều kiện để cache được: phần
/// này gần như không đổi (mã, chế độ, nhóm người) nên cache thoải mái, còn phần CHỌN RA AI thì phụ
/// thuộc con đếm lead đang tăng từng giây — cache vào là hỏng vòng chia.
/// </summary>
public sealed record CauHinhChiaSoDto(Guid CampaignId, int AssignMode, IReadOnlyList<Guid> Assignees);
