using System.ComponentModel.DataAnnotations;

namespace TourKit.Api.Pages.Customers;

/// <summary>View-model form khách hàng (offcanvas thêm/sửa) — bám bộ trường hệ cũ.</summary>
public sealed class CustomerFormInput
{
    [Required(ErrorMessage = "Bắt buộc nhập họ tên")]
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int CustomerType { get; set; }

    // Doanh nghiệp (hiện động theo loại khách)
    public string? UnitName { get; set; }   // Tên đơn vị
    public string? TaxCode { get; set; }    // Mã số thuế

    public string? Gender { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }

    public string? Source { get; set; }
    public List<string> Tags { get; set; } = [];   // Thẻ — multi-select
    public string? MarketGroup { get; set; }
    public string? CollaboratorName { get; set; }   // CTV

    public string? City { get; set; }
    public string? Address { get; set; }
    public string? IdCardNumber { get; set; }        // CMT/Hộ chiếu
    public DateTimeOffset? PassportExpiry { get; set; }

    public string? Note { get; set; }
}
