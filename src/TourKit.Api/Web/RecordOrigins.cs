using TourKit.Application.Crm;

namespace TourKit.Api.Web;

/// <summary>Bản ghi TIỀN THÂN — nơi giữ lịch sử của giai đoạn trước khi chuyển đổi.</summary>
/// <param name="EntityName">Tên loại bản ghi gốc, dùng lại đúng quy ước của EntityComment/AiInsight.</param>
/// <param name="EntityId">Khoá bản ghi gốc.</param>
/// <param name="Label">Gọi tên cho người đọc, ví dụ "khách tiềm năng".</param>
/// <param name="Url">Đường mở thẳng bản ghi gốc.</param>
public sealed record RecordOrigin(string EntityName, string EntityId, string Label, string Url);

/// <summary>
/// Tra NGƯỢC một bản ghi về bản ghi đã sinh ra nó.
///
/// Vì sao cần: chuyển đổi (khách tiềm năng → khách hàng) tạo một bản ghi MỚI với khoá mới. Mọi thứ
/// gắn theo cặp (EntityName, EntityId) — đánh giá AI, trao đổi — ở lại bên bản ghi cũ, nên trang
/// khách hàng mở ra trắng trơn và cũng không có đường nào quay về. Tri thức của cả quá trình bán
/// hàng bị bỏ lại đúng lúc khách trở nên quan trọng nhất.
///
/// Cách chữa là ĐỌC XUYÊN chứ không chép: chép thì nhân đôi dữ liệu, không cứu được những bản ghi
/// đã chuyển từ trước, và dòng "đã chấm lúc…" chép sang sẽ khiến người đọc tưởng là chấm cho khách
/// hàng trong khi nó chấm cho khách tiềm năng.
///
/// Nằm ở <c>Web</c> chứ không ở <c>Ai</c> vì cả trợ lý AI lẫn luồng trao đổi đều dùng — đây là quan
/// hệ giữa các bản ghi, không phải chuyện riêng của AI.
///
/// Hiện mới nối một chặng (khách hàng ← khách tiềm năng). Cơ hội → đơn hàng đi đúng khuôn này
/// (<c>ConvertedOrderId</c>) nhưng màn Đơn hàng CHƯA có khối nào tiêu thụ, nên chưa nối — thêm vào
/// lúc này là viết code không ai gọi. Khi cần chỉ thêm một nhánh ở đây.
/// </summary>
public sealed class RecordOrigins(ILeadService leads)
{
    /// <summary>Bản ghi gốc, hoặc <c>null</c> nếu bản ghi này không sinh ra từ chuyển đổi nào.</summary>
    public async Task<RecordOrigin?> FindAsync(string? entityName, string? entityId)
    {
        if (!Guid.TryParse(entityId, out var id) || entityName != "Customer")
        {
            return null;
        }

        var lead = await leads.FindByConvertedCustomerAsync(id).ConfigureAwait(false);
        return lead is null
            ? null
            : new RecordOrigin("Lead", lead.Id.ToString(), "khách tiềm năng", $"/khach-tiem-nang?mo={lead.Id}");
    }
}
