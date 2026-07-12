
namespace TourKit.Shared.Entities;

public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string? Code { get; set; }          // Mã KH (legacy KH_xxxxx) — tự sinh khi tạo
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int CustomerType { get; set; }      // legacy id_customer_type — 0 Cá nhân,1 Doanh nghiệp,2 Đối tác,3 CTV
    public string? Source { get; set; }        // nguồn khách (legacy Source)
    public string? Tag { get; set; }           // nhãn phân loại (legacy Tag)
    public decimal TempBalance { get; set; }   // tạm ứng (legacy TempBalance)

    // --- CRM bám Data khách hàng hệ cũ: gói TẤT CẢ field mềm + list vào 1 cột JSON.
    // ID tham chiếu (người tạo, NV phụ trách) lưu STRING để migrate được dữ liệu cũ (ID legacy không phải GUID).
    // Thêm field/list mới về sau KHÔNG cần migration. Serialize/deserialize ở CustomerService.
    public string? CrmProfileJson { get; set; }

    // --- Thông tin cá nhân/hộ chiếu (tour quốc tế: visa/vé bay). Additive nullable. ---
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? IdCardNumber { get; set; }          // CMND/CCCD
    public string? PassportNumber { get; set; }
    public DateTimeOffset? PassportExpiry { get; set; }
    public string? Nationality { get; set; }
}
