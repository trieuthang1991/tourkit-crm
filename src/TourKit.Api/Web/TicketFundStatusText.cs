using System.Globalization;
using System.Text.Json;
using TourKit.Shared.Entities;

namespace TourKit.Api.Web;

/// <summary>
/// Nhãn + màu badge cho trạng thái quỹ vé, lấy <see cref="TicketFundStatus"/> làm NGUỒN CHUẨN duy
/// nhất. Dùng chung cho: ô chọn ở form/lọc (<see cref="Options"/>), render Razor (<see cref="Label"/>
/// + <see cref="BadgeClass"/>), và lưới Tabulator phía client (<see cref="MapJson"/>).
///
/// Mã ngoài {0,1} (dữ liệu legacy tự do) hiển thị trung tính "Khác (n)" thay vì ném lỗi — trước đây
/// các màn để lộ số trần ("Trạng thái = 1") không ai hiểu; nay mọi nơi hiện nhãn.
/// </summary>
public static class TicketFundStatusText
{
    /// <summary>Các bậc trạng thái để đổ ô &lt;select&gt; (giá trị int + nhãn), theo thứ tự enum.</summary>
    public static IReadOnlyList<(int Value, string Label)> Options { get; } =
        Enum.GetValues<TicketFundStatus>().Select(s => ((int)s, LabelOf(s))).ToList();

    /// <summary>Nhãn tiếng Việt của một mã trạng thái; mã lạ trả "Khác (n)".</summary>
    public static string Label(int status) =>
        Enum.IsDefined(typeof(TicketFundStatus), status)
            ? LabelOf((TicketFundStatus)status)
            : $"Khác ({status})";

    /// <summary>Lớp badge Vuexy theo bậc: chưa dùng = xanh, đã dùng = vàng, mã lạ = xám.</summary>
    public static string BadgeClass(int status) => status switch
    {
        (int)TicketFundStatus.ChuaSuDung => "bg-label-success",
        (int)TicketFundStatus.DaSuDung => "bg-label-warning",
        _ => "bg-label-secondary",
    };

    /// <summary>Bản đồ {"mã": {t: nhãn, c: lớp badge}} cho formatter Tabulator dựng badge phía client.</summary>
    public static string MapJson { get; } = JsonSerializer.Serialize(
        Enum.GetValues<TicketFundStatus>().ToDictionary(
            s => ((int)s).ToString(CultureInfo.InvariantCulture),
            s => new { t = LabelOf(s), c = BadgeClass((int)s) }));

    private static string LabelOf(TicketFundStatus s) => s switch
    {
        TicketFundStatus.ChuaSuDung => "Chưa sử dụng",
        TicketFundStatus.DaSuDung => "Đã sử dụng",
        _ => s.ToString(),
    };
}
