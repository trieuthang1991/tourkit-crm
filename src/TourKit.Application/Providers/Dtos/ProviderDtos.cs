using TourKit.Shared.Enums;

namespace TourKit.Application.Providers.Dtos;

public sealed record ProviderDto(
    Guid Id, string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, Guid? PaymentTermId, int Rate, int Status,
    string? Province = null, Guid? BranchId = null, Guid? MarketTypeId = null,
    // Trường mềm riêng theo loại NCC (năm xây dựng, loại xe...). Xem ProviderProfile.
    ProviderProfile? Profile = null,
    // Công nợ NCC (bám danh sách hệ cũ): tổng mua (OrderCost) · đã trả (phiếu chi đã duyệt) · còn nợ. Chỉ ở danh sách.
    decimal TotalCost = 0m, decimal Paid = 0m, decimal Outstanding = 0m);

public sealed record CreateProviderDto(
    string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, Guid? PaymentTermId, int Rate, int Status,
    string? Province = null, Guid? BranchId = null, Guid? MarketTypeId = null, ProviderProfile? Profile = null);

public sealed record UpdateProviderDto(
    string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, Guid? PaymentTermId, int Rate, int Status,
    string? Province = null, Guid? BranchId = null, Guid? MarketTypeId = null, ProviderProfile? Profile = null);

/// <summary>
/// Một dòng sản phẩm/dịch vụ nằm TRONG form sửa nhà cung cấp.
///
/// Bám hệ cũ: <c>EditHotel.aspx</c> có panel "SẢN PHẨM/DỊCH VỤ" với các dòng thêm động, và
/// <c>uspInsertHotel</c> nhận cả danh sách đó rồi ghi chung một transaction — hỏng giữa chừng thì
/// ROLLBACK sạch. Tách ra lưu từng dòng là bỏ mất ràng buộc nguyên tử mà hệ cũ cố ý có.
///
/// <paramref name="Id"/> rỗng = dòng mới. Dòng có sẵn mà KHÔNG nằm trong danh sách gửi lên = đã xoá.
/// </summary>
public sealed record ProviderServiceLineDto(
    Guid? Id, Guid? ServiceItemId, string? PriceName,
    decimal ContractPrice, decimal PublicPrice, string? CurrencyCode,
    int AmountOfPeople, string? Note, int Status);

/// <summary>Bộ lọc danh sách NCC (bám hệ cũ). Tất cả optional.</summary>
public sealed record ProviderListFilter(
    string? Q = null, int? Type = null, int? Status = null,
    string? Province = null, Guid? BranchId = null, Guid? MarketTypeId = null,
    DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedTo = null);

/// <summary>Thẻ thống kê đầu màn NCC: tổng + đang hoạt động + ngừng.</summary>
public sealed record ProviderStatsDto(int Total, int Active, int Inactive);
