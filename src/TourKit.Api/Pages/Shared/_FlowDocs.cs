namespace TourKit.Api.Pages.Shared;

/// <summary>Một mục tài liệu luồng nghiệp vụ, gắn với một trang danh sách.</summary>
/// <param name="Key">Khoá partial nhận vào, ví dụ <c>"lead"</c>.</param>
/// <param name="Title">Đầu đề hiện trong popover.</param>
/// <param name="Summary">Một đến hai câu giới thiệu tính năng — chính là nội dung tooltip.</param>
/// <param name="Doc">Tên file trong <c>docs/business/</c>, không kèm đuôi <c>.md</c>.</param>
/// <param name="Anchor">Neo ASCII trong file đó (thẻ <c>&lt;a id="..."&gt;</c> đặt trên tiêu đề).</param>
public sealed record FlowDoc(string Key, string Title, string Summary, string Doc, string Anchor);

/// <summary>
/// Sổ đăng ký tài liệu luồng nghiệp vụ — ánh xạ một trang danh sách sang mục tương ứng trong
/// <c>docs/business/*.md</c>.
///
/// Vì sao có: đứng trên một trang danh sách đang chạy thì không đọc ra được dữ liệu ở đó SINH TỪ
/// ĐÂU và ĐI TIẾP ĐÂU. Những câu đó có lời giải trong mã, nhưng không ai đào ra được khi đang ở
/// trên giao diện.
///
/// Bám khuôn <see cref="MenuData"/>: một danh sách tĩnh, sửa một dòng là thêm được một trang.
/// Nội dung thật nằm trong markdown; ở đây chỉ giữ ÁNH XẠ, nên đổi lời văn không phải đụng vào mã.
///
/// Có test soát rằng mọi <see cref="FlowDoc.Anchor"/> thật sự tồn tại trong file markdown tương
/// ứng — đổi tên mục bên kia mà quên sửa ở đây thì đỏ ngay, thay vì để người dùng bấm vào link chết.
/// </summary>
public static class FlowDocs
{
    private const string Crm = "luong-crm";
    private const string Ncc = "luong-nha-cung-cap";

    public static readonly IReadOnlyList<FlowDoc> All = new List<FlowDoc>
    {
        // ---- CRM ----
        new("chia-so-sale", "Chia số Sale",
            "Phân khách đến cho nhân viên kinh doanh theo lượt, để không ai bị bỏ sót và không hai người cùng gọi một khách.",
            Crm, "chia-so-sale"),

        new("lead", "Khách tiềm năng",
            "Người đã để lại liên hệ nhưng chưa lượng hoá thành tiền. Chốt được thì chuyển thành khách hàng.",
            Crm, "khach-tiem-nang"),

        new("opportunity", "Cơ hội bán hàng",
            "Một lần khách hỏi mua đã lượng hoá thành tiền: số khách, đơn giá, giá trị dự kiến. Chốt được thì thành đơn hàng.",
            Crm, "co-hoi-ban-hang"),

        new("opportunity-report", "Báo cáo phễu cơ hội",
            "Gộp hai câu hỏi cạnh nhau: ai chốt được bao nhiêu, và vì sao phần còn lại không chốt.",
            Crm, "bao-cao-pheu-co-hoi"),

        new("customer", "Data khách hàng",
            "Hồ sơ khách dùng chung cho mọi module. Là nơi mọi nhánh bán hàng gặp nhau.",
            Crm, "data-khach-hang"),

        new("customer-dedup", "Rà khách trùng",
            "Tìm các hồ sơ khách trùng nhau do nhập nhiều lần hoặc nhiều nguồn đổ về, để gộp lại.",
            Crm, "ra-khach-trung"),

        new("care", "Quản lý lịch hẹn",
            "Việc cần làm với khách theo mốc thời gian: gọi lại, gửi báo giá, nhắc thanh toán.",
            Crm, "quan-ly-lich-hen"),

        new("rating", "Feedback chung",
            "Đánh giá khách gửi về, không gắn với một tour cụ thể.",
            Crm, "feedback-chung"),

        new("rating-tour", "Feedback theo Tour",
            "Đánh giá gắn với một tour, để so được chất lượng giữa các tour và các lần khởi hành.",
            Crm, "feedback-theo-tour"),

        // ---- Nhà cung cấp ----
        new("provider", "Nhà cung cấp",
            "Đối tác bán dịch vụ đầu vào: khách sạn, nhà xe, hàng không, nhà hàng. Gốc của mọi giá vốn.",
            Ncc, "nha-cung-cap"),

        new("service-catalog", "Danh mục dịch vụ",
            "Các dịch vụ một nhà cung cấp bán. Là thứ được chọn khi dựng giá tour và khi đặt dịch vụ.",
            Ncc, "danh-muc-dich-vu"),

        new("provider-price", "Bảng giá NCC",
            "Giá đầu vào theo dịch vụ và theo khoảng thời gian. Quyết định giá vốn của tour.",
            Ncc, "bang-gia-ncc"),

        new("payment-term", "Điều khoản TT NCC",
            "Thoả thuận thanh toán với từng nhà cung cấp: đặt cọc bao nhiêu, còn lại trả khi nào.",
            Ncc, "dieu-khoan-thanh-toan"),

        new("ticket-fund", "Series Vé / Quỹ vé",
            "Loạt chỗ đã giữ trước với hãng bay, trừ dần khi bán. Hết quỹ là hết chỗ bán.",
            Ncc, "quy-ve"),
    };

    /// <summary>Tra một mục theo khoá; <c>null</c> nếu khoá không có trong sổ.</summary>
    public static FlowDoc? Find(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : All.FirstOrDefault(d => d.Key == key);
}
