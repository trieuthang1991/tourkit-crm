using TourKit.Shared.Enums;

namespace TourKit.Application.Crm;

/// <summary>
/// Luật chia số: lead về từ một chiến dịch thì giao cho AI trong nhóm.
///
/// Tách thành HÀM THUẦN — không đụng CSDL, không đụng thời gian — vì đây là chỗ quyết định công
/// việc của người ta, và một quyết định như vậy phải kiểm thử được cạn kiệt. Nằm trong một hàm
/// private của service thì nó chỉ được chạy qua khi có lead thật đi vào.
/// </summary>
public static class ChiaSoSale
{
    /// <summary>
    /// Chọn người nhận. <c>null</c> khi không chia được (nhóm rỗng, hoặc chế độ không tự chia) —
    /// lúc đó lead để trống người phụ trách chứ KHÔNG ném lỗi: một lead về được mà không ai nhận
    /// vẫn hơn là mất luôn cái lead.
    /// </summary>
    /// <param name="mode">Chế độ chia của chiến dịch.</param>
    /// <param name="nhom">Nhóm nhân viên, ĐÃ sắp theo thứ tự vòng chia.</param>
    /// <param name="soLeadDaCo">Số lead chiến dịch đã có — con đếm để xoay vòng.</param>
    /// <param name="ngauNhien">Nguồn ngẫu nhiên; để trống thì dùng mặc định. Test truyền vào để cố định kết quả.</param>
    public static Guid? Chon(
        LeadAssignMode mode,
        IReadOnlyList<Guid> nhom,
        int soLeadDaCo,
        Func<int, int>? ngauNhien = null)
    {
        ArgumentNullException.ThrowIfNull(nhom);

        if (nhom.Count == 0 || mode == LeadAssignMode.KhongTuChia)
        {
            return null;
        }

        return mode switch
        {
            // Đếm rồi chia dư — KHÔNG giữ con trỏ "người kế tiếp" trong CSDL. Con trỏ phải
            // đọc–sửa–ghi mỗi lần có lead: hai lead về cùng lúc là lệch, và xoá một lead thì con trỏ
            // sai vĩnh viễn. Cách này tự đúng lại sau mọi biến động.
            LeadAssignMode.XoayVong => nhom[Math.Abs(soLeadDaCo) % nhom.Count],

            LeadAssignMode.NgauNhien => nhom[(ngauNhien ?? Random.Shared.Next)(nhom.Count)],

            _ => null,
        };
    }
}
