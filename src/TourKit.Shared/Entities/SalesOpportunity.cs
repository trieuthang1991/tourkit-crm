namespace TourKit.Shared.Entities;

/// <summary>
/// Cơ hội bán hàng — phiếu yêu cầu đi tour của khách, chạy qua phễu tới lúc chốt thành đơn.
/// Bám <c>BookingTicket</c> của hệ cũ (menu "Cơ hội bán hàng", đường <c>/booking-ticket</c>).
///
/// KHÁC HẲN <see cref="Lead"/>: Lead là số khách thô được chia cho sale (hệ cũ để ở menu riêng
/// "Chia số Sale") — chỉ có tên và điện thoại. Cơ hội là một NHU CẦU CỤ THỂ: đi tour nào, mấy
/// người, giá bao nhiêu. Không có số khách và giá thì không tính được giá trị phễu, mà đó gần như
/// là lý do màn này tồn tại.
///
/// Chốt đơn KHÔNG phải một hàm trên thực thể này. Hệ cũ làm ngược: luồng đặt khách lên tour nhận
/// thêm mã phiếu, tạo đơn xong MỚI quay lại đánh dấu phiếu đã chốt (xem
/// <c>uspInsertTourSampleCustomer_V4</c>). Làm ngược lại sẽ đẻ ra đường tạo đơn thứ hai, lệch luật
/// với đường đang có.
/// </summary>
public sealed class SalesOpportunity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Mã phiếu (legacy <c>CodePhieu</c>). Duy nhất theo tenant, không tái dùng sau khi xoá.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên phiếu (legacy <c>TenPhieu</c>) — vd "Chị Lan hỏi Đà Nẵng 4N3Đ tháng 9".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Nội dung yêu cầu của khách (legacy <c>NoiDungPhieu</c>), lưu HTML.</summary>
    public string? Content { get; set; }

    // ---- Khách hỏi ----
    // Giữ cả thông tin rời LẪN khoá ngoại: lúc mới nhận yêu cầu thường chưa có hồ sơ khách nào, chỉ
    // có mẩu giấy ghi tên với số điện thoại. Bắt tạo khách trước mới cho ghi cơ hội là chặn đúng
    // lúc người bán đang cần ghi nhanh.
    public string ContactName { get; set; } = string.Empty;   // TenKH
    public string? ContactPhone { get; set; }                 // SoDienThoaiKH
    public string? ContactEmail { get; set; }                 // EmailKH
    public string? ContactAddress { get; set; }               // DiaChiKH
    public Guid? CustomerId { get; set; }                     // IdKhachHang

    // ---- Nhu cầu: BỐN bậc khách, khớp TourCustomer ----
    // Hệ cũ chỉ có ba bậc (SoLuong/QuantityChild/QuantityBaby), nhưng đơn hàng của ta có bốn. Theo
    // ba bậc thì tới lúc chốt sẽ rơi mất bậc trẻ nhỏ — mà chốt đơn chính là đích của cơ hội.
    public int AdultQty { get; set; }
    public int ChildQty { get; set; }
    public int ChildSmallQty { get; set; }
    public int BabyQty { get; set; }

    public decimal PriceAdult { get; set; }
    public decimal PriceChild { get; set; }
    public decimal PriceChildSmall { get; set; }
    public decimal PriceBaby { get; set; }

    /// <summary>Mẫu tour khách đang hỏi (legacy <c>TourIdRoot</c>) — có thể chưa biết.</summary>
    public Guid? TemplateId { get; set; }

    /// <summary>Chuyến cụ thể nếu khách đã nhắm ngày (legacy <c>TourTypeRoot</c> đi kèm TourIdRoot).</summary>
    public Guid? TourDepartureId { get; set; }

    // ---- Phễu ----
    /// <summary>Trỏ tới <see cref="OpportunityStage.Code"/>, KHÔNG phải khoá ngoại theo Id: cột có
    /// thể bị đổi tên hay sắp lại mà cơ hội cũ vẫn phải giữ nguyên bước đang đứng.</summary>
    public int StageCode { get; set; } = OpportunityStageCode.TaoMoi;

    /// <summary>Lý do huỷ (danh mục <see cref="TransferReason"/>) — bắt buộc khi StageCode = Huỷ.</summary>
    public Guid? CancelReasonId { get; set; }
    public string? CancelNote { get; set; }

    /// <summary>Đơn sinh ra khi chốt. Có giá trị nghĩa là cơ hội đã chốt, không chốt lại được.</summary>
    public Guid? ConvertedOrderId { get; set; }

    // ---- Phân công ----
    // NHIỀU người phụ trách + người theo dõi, nhưng để ở BẢNG RIÊNG (SalesOpportunityAssignee), KHÔNG
    // nhét chuỗi id vào một cột như hệ cũ — xem chú thích ở lớp đó về ba thứ hỏng theo.
    public ICollection<SalesOpportunityAssignee> Assignees { get; set; } = [];
    public Guid? CreatedByUserId { get; set; }
    public Guid? BranchId { get; set; }

    // ---- Nguồn ----
    public Guid? CustomerSourceId { get; set; }   // CustomerSourceId → danh mục Nguồn khách
    public Guid? MarketTypeId { get; set; }       // MarketId → danh mục Thị trường
    public bool FromWebsite { get; set; }         // IsWebsite

    // ---- Xác nhận ----
    public bool IsConfirmed { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public Guid? ConfirmedByUserId { get; set; }

    /// <summary>
    /// Tệp đính kèm (legacy <c>TepDinhKems</c>): JSON mảng Guid trỏ sang <see cref="FileUpload"/>.
    ///
    /// Ở đây JSON là ĐÚNG, khác chỗ người phụ trách: tệp chỉ đọc kèm cơ hội, không bao giờ hỏi ngược
    /// "tệp này đang nằm ở cơ hội nào". Cùng quy ước với <see cref="EntityComment.AttachmentIds"/>.
    ///
    /// Hệ cũ lưu thẳng ĐƯỜNG DẪN vào cột text — đổi chỗ chứa tệp là hỏng toàn bộ liên kết cũ.
    /// </summary>
    public string? AttachmentIds { get; set; }

    // Giá trị dự kiến ("giá trị phễu") KHÔNG có cột riêng ở đây — xem OpportunityMath.GiaTriSelector.
    // Công thức nằm ở một chỗ duy nhất, dạng biểu thức dịch được sang SQL, nên vừa cộng/sắp/lọc được
    // ở CSDL vừa không có bản sao thứ hai để trôi lệch.
}
