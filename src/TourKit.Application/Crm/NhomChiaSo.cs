using System.Text.Json;

namespace TourKit.Application.Crm;

/// <summary>
/// Đọc/ghi nhóm nhân viên nhận số của một chiến dịch (<c>LeadCampaign.AssigneesJson</c>).
///
/// Thứ tự trong mảng CHÍNH LÀ thứ tự vòng chia — giữ nguyên, không sắp lại.
/// </summary>
public static class NhomChiaSo
{
    public static IReadOnlyList<Guid> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            // Chuỗi hỏng thì coi như chưa cấu hình nhóm: lead vẫn vào được, chỉ là chưa ai phụ
            // trách. Ném ở đây là chặn đường nhận lead của khách vì một lỗi dữ liệu nội bộ.
            return [];
        }
    }

    /// <summary>
    /// Chuẩn hoá rồi serialize: bỏ id rỗng, bỏ TRÙNG (giữ lần xuất hiện đầu để không đổi thứ tự
    /// vòng chia). Trùng tên trong nhóm nghĩa là người đó nhận gấp đôi phần mình đáng được, mà
    /// nhìn danh sách thì không thấy gì bất thường.
    /// </summary>
    public static string? ToJsonOrNull(IEnumerable<Guid>? ids)
    {
        if (ids is null)
        {
            return null;
        }

        var sach = new List<Guid>();
        var da = new HashSet<Guid>();
        foreach (var id in ids)
        {
            if (id != Guid.Empty && da.Add(id))
            {
                sach.Add(id);
            }
        }

        return sach.Count == 0 ? null : JsonSerializer.Serialize(sach);
    }
}
