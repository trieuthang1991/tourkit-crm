using System.Globalization;
using System.Text;
using TourKit.Application.Collaboration;
using TourKit.Application.Crm;
using TourKit.Application.Customers;

namespace TourKit.Api.Ai;

/// <summary>
/// Dựng HỒ SƠ của một bản ghi để gửi cho AI: các trường của bản ghi + luồng trao đổi nội bộ.
///
/// Dùng chung cho mọi tính năng đọc bản ghi (chấm điểm, tóm tắt, soạn tin). Ba tính năng cùng đọc một
/// bản ghi mà mỗi cái tự dựng hồ sơ theo cách riêng thì chúng sẽ dần lệch nhau, và tính năng này thấy
/// dữ liệu mà tính năng kia không thấy — người dùng đọc hai kết quả sẽ tưởng hệ thống mâu thuẫn.
///
/// Nằm ở tầng Api chứ không ở lõi AI vì nó gọi service nghiệp vụ; nhờ vậy bộ lọc theo công ty và ẩn
/// bản ghi đã xoá tự động áp dụng, AI không bao giờ thấy dữ liệu người hỏi không được xem.
/// </summary>
public sealed class AiRecordSheet(
    ILeadService leads,
    ICustomerService customers,
    IEntityCommentService comments)
{
    /// <summary>
    /// Số dòng trao đổi tối đa đưa vào hồ sơ. Có biên vì một cơ hội chăm sóc lâu có thể có hàng trăm
    /// dòng — đưa hết vào vừa tốn token vừa làm model loãng sự chú ý.
    /// </summary>
    public const int MaxComments = 30;

    /// <summary>Tên hiển thị của bản ghi, để đưa vào câu trả lời. <c>null</c> = không đọc được.</summary>
    public async Task<string?> TitleAsync(string entityName, string entityId)
    {
        if (!Guid.TryParse(entityId, out var id))
        {
            return null;
        }

        return entityName switch
        {
            "Lead" => (await leads.GetAsync(id).ConfigureAwait(false)).FullName,
            "Customer" => (await customers.GetAsync(id).ConfigureAwait(false)).FullName,
            _ => null,
        };
    }

    /// <summary>Hồ sơ đầy đủ. <c>null</c> khi loại bản ghi không hỗ trợ hoặc id không hợp lệ.</summary>
    public async Task<string?> BuildAsync(string entityName, string entityId)
    {
        if (!Guid.TryParse(entityId, out var id))
        {
            return null;
        }

        var sb = new StringBuilder();

        switch (entityName)
        {
            case "Lead":
                var lead = await leads.GetAsync(id).ConfigureAwait(false);
                sb.Append("HỒ SƠ CƠ HỘI BÁN HÀNG\n")
                  .Append("- Tên khách: ").Append(lead.FullName).Append('\n')
                  .Append("- Nguồn: ").Append(Or(lead.Source)).Append('\n')
                  .Append("- Trạng thái: ").Append(lead.Status).Append('\n')
                  .Append("- Có số điện thoại: ").Append(Yes(lead.Phone)).Append('\n')
                  .Append("- Có email: ").Append(Yes(lead.Email)).Append('\n')
                  .Append("- Đã chuyển thành khách hàng: ").Append(lead.ConvertedCustomerId is null ? "chưa" : "rồi").Append('\n');
                break;

            case "Customer":
                var customer = await customers.GetAsync(id).ConfigureAwait(false);
                sb.Append("HỒ SƠ KHÁCH HÀNG\n")
                  .Append("- Tên: ").Append(customer.FullName).Append('\n')
                  .Append("- Nguồn: ").Append(Or(customer.Source)).Append('\n')
                  .Append("- Nhóm thị trường: ").Append(Or(customer.MarketGroup)).Append('\n')
                  .Append("- Nhu cầu ban đầu: ").Append(Or(customer.InitialNeed)).Append('\n')
                  .Append("- Thành phố: ").Append(Or(customer.City)).Append('\n')
                  .Append("- Số dư tạm: ").Append(Money(customer.TempBalance)).Append(" đồng\n")
                  .Append("- Phân khúc: ").Append(Join(customer.Segments)).Append('\n')
                  .Append("- Nhãn: ").Append(Join(customer.Tags)).Append('\n')
                  .Append("- Ghi chú: ").Append(Or(customer.Note)).Append('\n');
                break;

            default:
                return null;
        }

        var thread = await comments.ListAsync(entityName, entityId, MaxComments).ConfigureAwait(false);
        sb.Append("\nDIỄN BIẾN TRAO ĐỔI NỘI BỘ (mới nhất trước, tối đa ")
          .Append(MaxComments.ToString(CultureInfo.InvariantCulture)).Append(" dòng):\n");

        if (thread.Count == 0)
        {
            sb.Append("(chưa có trao đổi nào — đây là một thiếu sót dữ liệu, hãy tính đến khi trả lời)\n");
        }
        else
        {
            foreach (var c in thread)
            {
                sb.Append("- [").Append(c.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).Append("] ")
                  .Append(c.AuthorName).Append(": ").Append(c.Content.ReplaceLineEndings(" ")).Append('\n');
            }
        }

        return sb.ToString();
    }

    private static string Money(decimal value) =>
        value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');

    private static string Or(string? value) => string.IsNullOrWhiteSpace(value) ? "(chưa có)" : value;

    private static string Yes(string? value) => string.IsNullOrWhiteSpace(value) ? "không" : "có";

    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "(chưa có)" : string.Join(", ", values);
}
