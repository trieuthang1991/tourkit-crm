namespace TourKit.Shared.Enums;

/// <summary>Cách chia số của một chiến dịch thu lead cho nhóm nhân viên.</summary>
public enum LeadAssignMode
{
    /// <summary>Không tự chia — lead về để trống người phụ trách, ai đó gán tay sau.</summary>
    KhongTuChia = 0,

    /// <summary>
    /// Xoay vòng: người thứ nhất, thứ hai, thứ ba… rồi quay lại. Chia đều tuyệt đối.
    ///
    /// Tính bằng <c>số lead đã có của chiến dịch % số người trong nhóm</c> — KHÔNG giữ con trỏ
    /// "người kế tiếp" trong CSDL. Con trỏ thì phải đọc–sửa–ghi mỗi lần có lead, hai lead về cùng
    /// lúc là lệch, và xoá một lead thì con trỏ sai vĩnh viễn. Cách đếm này tự đúng lại.
    /// </summary>
    XoayVong = 1,

    /// <summary>Ngẫu nhiên trong nhóm — dùng khi không muốn đoán trước ai nhận số nào.</summary>
    NgauNhien = 2,
}

/// <summary>Nhãn tiếng Việt — dùng chung cho giao diện, để không lọt tên enum ra trước người dùng.</summary>
public static class LeadAssignModeText
{
    public static string Vi(LeadAssignMode m) => m switch
    {
        LeadAssignMode.XoayVong => "Xoay vòng",
        LeadAssignMode.NgauNhien => "Ngẫu nhiên",
        LeadAssignMode.KhongTuChia => "Không tự chia",
        _ => "—",
    };
}
