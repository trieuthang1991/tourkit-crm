using System.Globalization;
using System.Text;
using TourKit.Application.Collaboration;
using TourKit.Application.Common;
using TourKit.Application.Crm;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;
using TourKit.Application.Sales;

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
    ISalesOpportunityService opportunities,
    IEntityCommentService comments,
    TourKit.Application.Ai.IAiInsightStore insights,
    IHttpContextAccessor http)
{
    /// <summary>
    /// Người đang hỏi có được xem hồ sơ khách hàng không.
    ///
    /// Dùng để chia đôi phần ngữ cảnh khách hàng gắn với cơ hội: phần HỒ SƠ (tên, nguồn, phân khúc)
    /// là ngữ cảnh của chính cơ hội — form cơ hội đã hiện sẵn tên và số điện thoại khách — nên lấy
    /// thẳng. Còn phần NHẬN ĐỊNH đã lưu của khách thì là một khối nội dung riêng, viết trong ngữ
    /// cảnh mà người phụ trách cơ hội có thể không dự phần (công nợ, phàn nàn), nên phải có quyền.
    /// </summary>
    private bool DuocXemKhach =>
        http.HttpContext?.User.HasClaim("perm", TourKit.Api.Authz.Permissions.CustomerView) == true;

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
            "SalesOpportunity" => (await opportunities.GetAsync(id).ConfigureAwait(false)).Title,
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
                // Tiêu đề nói ĐÚNG loại bản ghi. Trước đây ghi "CƠ HỘI BÁN HÀNG" theo cách màn hình
                // gọi tên, nhưng nay cơ hội bán hàng là một loại bản ghi RIÊNG cũng được chấm điểm —
                // hai hồ sơ mở đầu giống hệt nhau thì model không phân biệt nổi đang đọc cái nào.
                sb.Append("HỒ SƠ KHÁCH TIỀM NĂNG (lead — chưa có báo giá, chưa có giá trị tiền)\n")
                  .Append("- Tên khách: ").Append(lead.FullName).Append('\n')
                  .Append("- Nguồn: ").Append(Or(lead.Source)).Append('\n')
                  .Append("- Trạng thái: ").Append(TourKit.Shared.Enums.LeadStatusText.Vi(lead.Status)).Append('\n')
                  .Append("- Có số điện thoại: ").Append(Yes(lead.Phone)).Append('\n')
                  .Append("- Có email: ").Append(Yes(lead.Email)).Append('\n')
                  .Append("- Đã chuyển thành khách hàng: ").Append(lead.ConvertedCustomerId is null ? "chưa" : "rồi").Append('\n')
                  // Nhu cầu khách tự nêu — thứ quyết định nhất trong cả hồ sơ này. Thiếu nó thì mọi
                  // nhận định đều phải đoán, và phần chấm điểm chỉ còn biết kết luận "hồ sơ trống".
                  .Append("- Nhu cầu khách nêu: ").Append(Or(lead.Note)).Append('\n');

                if (lead.Attribution is { } ng)
                {
                    var nguon = new[]
                    {
                        ng.UtmSource is null ? null : "utm_source=" + ng.UtmSource,
                        ng.UtmMedium is null ? null : "utm_medium=" + ng.UtmMedium,
                        ng.UtmCampaign is null ? null : "utm_campaign=" + ng.UtmCampaign,
                        ng.LandingPage is null ? null : "trang đích=" + ng.LandingPage,
                    }.Where(x => x is not null).ToList();

                    if (nguon.Count > 0)
                    {
                        sb.Append("- Nguồn chi tiết: ").Append(string.Join(" · ", nguon)).Append('\n');
                    }
                }
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

            case "SalesOpportunity":
                var opp = await opportunities.GetAsync(id).ConfigureAwait(false);

                // Tên giai đoạn lấy từ DANH MỤC chứ không hardcode: các bước giữa phễu do người dùng
                // tự đặt, ghi cứng ở đây thì hồ sơ gửi AI sẽ nói sai tên bước ngay khi họ đổi.
                var stages = await opportunities.ListStagesAsync().ConfigureAwait(false);
                var stage = stages.FirstOrDefault(s => s.Code == opp.StageCode)?.Name;
                var khach = opp.AdultQty + opp.ChildQty + opp.ChildSmallQty + opp.BabyQty;

                sb.Append("HỒ SƠ CƠ HỘI BÁN HÀNG (đã lượng hoá thành tiền)\n")
                  .Append("- Mã: ").Append(opp.Code).Append('\n')
                  .Append("- Tiêu đề: ").Append(opp.Title).Append('\n')
                  .Append("- Giai đoạn: ").Append(Or(stage)).Append('\n')
                  .Append("- Nhu cầu khách nêu: ").Append(Or(opp.Content)).Append('\n')
                  .Append("- Người liên hệ: ").Append(Or(opp.ContactName)).Append('\n')
                  .Append("- Có số điện thoại: ").Append(Yes(opp.ContactPhone)).Append('\n')
                  .Append("- Có email: ").Append(Yes(opp.ContactEmail)).Append('\n')
                  .Append("- Số khách: ").Append(khach.ToString(CultureInfo.InvariantCulture))
                    .Append(" (người lớn ").Append(opp.AdultQty.ToString(CultureInfo.InvariantCulture))
                    .Append(", trẻ em ").Append(opp.ChildQty.ToString(CultureInfo.InvariantCulture))
                    .Append(", trẻ nhỏ ").Append(opp.ChildSmallQty.ToString(CultureInfo.InvariantCulture))
                    .Append(", em bé ").Append(opp.BabyQty.ToString(CultureInfo.InvariantCulture)).Append(")\n")
                  .Append("- Giá trị ước tính: ").Append(Money(opp.EstimatedValue)).Append(" đồng\n")
                  // Ba mốc dưới là THƯỚC ĐO cơ hội đã đi được bao xa, quan trọng hơn bản thân cái id:
                  // chọn được mẫu tour nghĩa là đã khoanh được sản phẩm, gắn được ngày khởi hành nghĩa
                  // là đã chốt được lịch. Gửi id thô sang model thì nó chỉ thấy một chuỗi vô nghĩa.
                  .Append("- Đã chọn mẫu tour khách hỏi: ").Append(opp.TemplateId is null ? "chưa" : "rồi").Append('\n')
                  .Append("- Đã gắn ngày khởi hành cụ thể: ").Append(opp.TourDepartureId is null ? "chưa" : "rồi").Append('\n')
                  .Append("- Đã xác nhận: ").Append(opp.IsConfirmed ? "rồi" : "chưa").Append('\n')
                  .Append("- Đã chuyển thành đơn hàng: ").Append(opp.ConvertedOrderId is null ? "chưa" : "rồi").Append('\n')
                  .Append("- Khách tự vào từ website: ").Append(opp.FromWebsite ? "có" : "không").Append('\n');

                await ThemKhachGanAsync(sb, opp.CustomerId).ConfigureAwait(false);
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

    /// <summary>
    /// Gắn ngữ cảnh KHÁCH HÀNG đã gán vào hồ sơ cơ hội. Một cơ hội đứng một mình chỉ kể được lần
    /// hỏi này; cùng một yêu cầu từ khách đã đi bốn chuyến khác hẳn từ người lạ chưa mua bao giờ.
    /// </summary>
    private async Task ThemKhachGanAsync(StringBuilder sb, Guid? customerId)
    {
        if (customerId is not { } khachId)
        {
            return;
        }

        CustomerDto kh;
        try
        {
            kh = await customers.GetAsync(khachId).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            // Khách đã bị xoá mềm: cơ hội vẫn còn trỏ tới. Bỏ qua phần ngữ cảnh chứ không làm hỏng
            // cả lượt chấm — hồ sơ cơ hội tự nó đã đủ để chấm.
            return;
        }

        sb.Append("\nKHÁCH HÀNG ĐÃ GÁN VỚI CƠ HỘI NÀY\n")
          .Append("- Tên: ").Append(kh.FullName).Append('\n')
          .Append("- Nguồn: ").Append(Or(kh.Source)).Append('\n')
          .Append("- Nhóm thị trường: ").Append(Or(kh.MarketGroup)).Append('\n')
          .Append("- Phân khúc: ").Append(Join(kh.Segments)).Append('\n')
          .Append("- Nhãn: ").Append(Join(kh.Tags)).Append('\n')
          .Append("- Số dư tạm: ").Append(Money(kh.TempBalance)).Append(" đồng\n");

        if (!DuocXemKhach)
        {
            return;
        }

        var daCham = await insights.LatestAsync("Customer", khachId.ToString()).ConfigureAwait(false);
        var review = daCham.FirstOrDefault(i => i.Kind == "Review");
        if (review?.Summary is { Length: > 0 } nhanDinh)
        {
            // CỐ Ý không đưa ĐIỂM SỐ cũ vào đây, chỉ đưa nhận định.
            // Thấy "khách này từng được chấm 82" thì model sẽ neo vào con số đó và chấm lại ra xấp
            // xỉ như vậy, bất kể cơ hội đang xét tốt hay xấu — diễn biến điểm thành một đường phẳng
            // giả. Nhận định thì là SỰ KIỆN để suy xét, không phải một đáp án có sẵn để chép lại.
            sb.Append("- Nhận định đã có về khách (dùng làm bối cảnh, KHÔNG phải điểm của cơ hội này): ")
              .Append(nhanDinh.ReplaceLineEndings(" ")).Append('\n');
        }
    }

    private static string Money(decimal value) =>
        value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');

    private static string Or(string? value) => string.IsNullOrWhiteSpace(value) ? "(chưa có)" : value;

    private static string Yes(string? value) => string.IsNullOrWhiteSpace(value) ? "không" : "có";

    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "(chưa có)" : string.Join(", ", values);
}
