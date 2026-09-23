namespace TourKit.Shared.Enums;

public enum LeadStatus
{
    New = 1,
    Contacted = 2,
    Qualified = 3,
    Won = 4,
    Lost = 5,
}

/// <summary>Nhãn tiếng Việt của trạng thái khách tiềm năng — DÙNG CHUNG (UI + hồ sơ gửi AI) để
/// không nơi nào lọt tên enum tiếng Anh ("Lost"/"Won") ra trước người dùng.</summary>
public static class LeadStatusText
{
    public static string Vi(LeadStatus s) => s switch
    {
        LeadStatus.New => "Mới",
        LeadStatus.Contacted => "Đã liên hệ",
        LeadStatus.Qualified => "Tiềm năng",
        LeadStatus.Won => "Thành công (đã chốt)",
        LeadStatus.Lost => "Thất bại (mất khách)",
        _ => "—",
    };
}
