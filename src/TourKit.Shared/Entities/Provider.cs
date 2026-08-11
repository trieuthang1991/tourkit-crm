
using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>
/// Nhà cung cấp dịch vụ (khách sạn, xe, nhà hàng, HDV, hãng bay...) — grounded ở legacy bảng `providers`.
/// Legacy còn class_hotel_id/car_type/HDV fields (Skill/Languages/Gender) theo từng loại NCC — deferred,
/// chưa cần cho slice chi phí MVP.
/// </summary>
public sealed class Provider : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProviderType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public Guid? PaymentTermId { get; set; }       // điều khoản thanh toán NCC (legacy ServicePaymentTerm)
    public string? Province { get; set; }          // Tỉnh thành (lọc theo địa bàn)
    public Guid? BranchId { get; set; }            // Chi nhánh (legacy ChiNhanh)
    public Guid? MarketTypeId { get; set; }        // Thị trường (legacy MarketType)
    public int Rate { get; set; }
    public int Status { get; set; }

    /// <summary>
    /// Trường mềm riêng theo loại NCC (năm xây dựng, quốc gia, loại xe...) — hệ cũ để ở đầu form
    /// nhưng mỗi loại một bộ khác nhau. Xem <c>TourKit.Application.Providers.ProviderProfile</c>.
    /// Chỉ để hiển thị/sửa; thứ cần lọc ở SQL phải là cột thật.
    /// </summary>
    public string? ProfileJson { get; set; }
}
