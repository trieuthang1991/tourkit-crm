using System.Text.Json;
using System.Text.Json.Serialization;
using TourKit.Shared.Enums;

namespace TourKit.Application.Providers;

/// <summary>
/// Một người liên hệ của nhà cung cấp — hệ cũ để thành khối "Thông tin liên hệ" lặp lại ở cả 4 màn
/// sửa (<c>foreach (var item in dataServices)</c> dựng một <c>.row.member</c> mỗi người).
///
/// Cột <c>Provider.ContactPerson</c> vẫn giữ: danh sách NCC hiển thị và TÌM KIẾM theo nó ở SQL, mà
/// tìm trong JSON là quét cả bảng. Nó mang tên người liên hệ chính; danh sách này mang đầy đủ.
/// </summary>
public sealed record ProviderContact
{
    public string? FullName { get; init; }
    public string? Position { get; init; }      // "Chức vụ"
    public DateOnly? DateOfBirth { get; init; } // "Ngày sinh"
    public string? Phone { get; init; }
    public string? Email { get; init; }

    /// <summary>Dòng không có gì thì đừng lưu — người dùng bấm "Thêm" rồi bỏ trống là chuyện thường.</summary>
    public bool Rong =>
        string.IsNullOrWhiteSpace(FullName) && string.IsNullOrWhiteSpace(Position) &&
        DateOfBirth is null && string.IsNullOrWhiteSpace(Phone) && string.IsNullOrWhiteSpace(Email);
}

/// <summary>
/// Trường "mềm" của nhà cung cấp — thứ hệ cũ có ở ĐẦU form sửa nhưng khác nhau theo từng loại NCC,
/// gộp trong một cột JSON (<c>Provider.ProfileJson</c>) thay vì mỗi loại một cột.
///
/// Vì sao JSON: hệ cũ có 4 màn sửa riêng (<c>Edit</c>, <c>EditHotel</c>, <c>EditPlaneTicket</c>,
/// <c>EditVoucher</c>) dùng chung ~14 trường và mỗi màn thêm vài trường của riêng nó. Đổ hết thành
/// cột thật thì bảng providers có một loạt cột luôn null với 5/6 loại, và mỗi lần hệ cũ thêm một
/// trường cho MỘT loại lại phải migration. Giống <see cref="Customers.CustomerCrmProfile"/>.
///
/// Cột thật vẫn giữ cho thứ cần LỌC/SẮP XẾP ở SQL (Province, MarketTypeId, Rate...). Đừng đưa vào
/// đây trường mà danh sách cần lọc theo — lọc trong JSON là quét cả bảng.
/// </summary>
public sealed record ProviderProfile
{
    // ----- Chung cho mọi loại (hệ cũ có, thực thể chưa có cột) -----

    public string? Website { get; init; }           // "Link"
    public string? Note { get; init; }              // "Ghi chú"
    public string? BankAccountName { get; init; }   // "Tên TK" — khác BankAccount (số TK) và BankName

    /// <summary>"Thông tin liên hệ" — hệ cũ cho khai NHIỀU người trên mọi loại NCC.</summary>
    public IReadOnlyList<ProviderContact> Contacts { get; init; } = [];

    // ----- Khách sạn (EditHotel.aspx) -----

    public int? BuiltYear { get; init; }            // "Năm xây dựng"
    public string? Country { get; init; }           // "Quốc gia"

    // ----- Vận chuyển (Edit.aspx) -----

    /// <summary>"Thông tin xe": xe nhà hay xe đối tác. Xem <see cref="VehicleOwnerships"/>.</summary>
    public string? VehicleOwnership { get; init; }

    /// <summary>"Loại xe" — hệ cũ cho chọn NHIỀU hạng ghế (4/7/16/29/35/45/47 chỗ) trên một NCC.</summary>
    public IReadOnlyList<string> VehicleTypes { get; init; } = [];

    /// <summary>Hai lựa chọn của "Thông tin xe" ở <c>Edit.aspx</c>.</summary>
    public static readonly IReadOnlyList<string> VehicleOwnerships = ["Xe nhà", "Xe đối tác"];

    /// <summary>Hạng ghế hệ cũ liệt kê sẵn ở <c>Edit.aspx</c>.</summary>
    public static readonly IReadOnlyList<string> VehicleTypeOptions =
        ["4 chỗ", "7 chỗ", "16 chỗ", "29 chỗ", "35 chỗ", "45 chỗ", "47 chỗ"];

    // ----- Voucher (EditVoucher.aspx) -----

    /// <summary>"Class Hotel" — hạng khách sạn của gói voucher. Hệ cũ tra từ danh mục ClassHotel;
    /// ở đây lưu chuỗi mềm, nâng thành liên kết thực thể sau nếu cần lọc/báo cáo theo nó.</summary>
    public string? HotelClass { get; init; }

    /// <summary>"Tên dự án".</summary>
    public string? ProjectName { get; init; }

    /// <summary>Loại NCC nào hiện trường riêng nào — dùng chung cho cả giao diện lẫn kiểm thử.</summary>
    public static bool CoTruongKhachSan(ProviderType t) => t == ProviderType.Hotel;

    public static bool CoTruongXe(ProviderType t) => t == ProviderType.Vehicle;

    public static bool CoTruongVoucher(ProviderType t) => t == ProviderType.Voucher;

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static ProviderProfile Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ProviderProfile();
        }

        try
        {
            return JsonSerializer.Deserialize<ProviderProfile>(json, Options) ?? new ProviderProfile();
        }
        catch (JsonException)
        {
            return new ProviderProfile();
        }
    }

    /// <summary>Serialize; null nếu rỗng hoàn toàn (không lưu JSON thừa).</summary>
    public string? ToJsonOrNull()
    {
        var empty = Website is null && Note is null && BankAccountName is null &&
            BuiltYear is null && Country is null && VehicleOwnership is null &&
            HotelClass is null && ProjectName is null &&
            VehicleTypes.Count == 0 && Contacts.Count == 0;
        return empty ? null : JsonSerializer.Serialize(this, Options);
    }
}
