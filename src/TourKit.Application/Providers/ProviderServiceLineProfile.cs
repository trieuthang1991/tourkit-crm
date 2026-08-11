using System.Text.Json;
using System.Text.Json.Serialization;
using TourKit.Shared.Enums;

namespace TourKit.Application.Providers;

/// <summary>
/// Trường riêng của MỘT DÒNG bảng giá, khác nhau theo loại NCC — lưu gộp trong
/// <c>ProviderService.ProfileJson</c>.
///
/// Khác <see cref="ProviderProfile"/> ở chỗ đứng: hệ cũ có hai mức trường riêng. Đối chiếu vị trí
/// nhãn so với panel bảng giá của 4 màn sửa cho thấy "Năm xây dựng"/"Quốc gia" nằm ở ĐẦU form (mức
/// NCC), còn "Giai đoạn từ/Đến", "Loại ngày", "Hành trình vé", "Hạn cắt cọc" nằm trong hàng tiêu đề
/// của bảng giá (mức DÒNG) — tức mỗi gói giá một bộ giá trị riêng.
///
/// Giá và số lượng vẫn là cột thật (ContractPrice/PublicPrice/AmountOfPeople): báo cáo công nợ và
/// giá vốn cộng theo chúng ở SQL. Chỉ thứ không phải cộng/lọc mới vào đây.
/// </summary>
public sealed record ProviderServiceLineProfile
{
    // ----- Khách sạn (EditHotel.aspx: hàng tiêu đề bảng giá) -----

    /// <summary>"Giai đoạn từ" — mốc ngày, không giờ.</summary>
    public DateOnly? PeriodFrom { get; init; }

    /// <summary>"Đến".</summary>
    public DateOnly? PeriodTo { get; init; }

    /// <summary>"Loại ngày". Xem <see cref="DayTypes"/>.</summary>
    public string? DayType { get; init; }

    /// <summary>"Chi phí NET/Ngày".</summary>
    public decimal? NetCostPerDay { get; init; }

    /// <summary>"Giá bán/Ngày".</summary>
    public decimal? SellPricePerDay { get; init; }

    // ----- Vé máy bay (EditPlaneTicket.aspx) -----

    /// <summary>"Loại vé". Xem <see cref="TicketTypes"/>.</summary>
    public string? TicketType { get; init; }

    /// <summary>"Hành trình vé" — VD "SGN-HAN-SGN".</summary>
    public string? Route { get; init; }

    /// <summary>"Giờ đi" / "Giờ về" — giờ bay, không kèm ngày (ngày nằm ở chuyến cụ thể).</summary>
    public string? DepartTime { get; init; }

    public string? ReturnTime { get; init; }

    /// <summary>"Hạn cắt cọc".</summary>
    public DateOnly? DepositDeadline { get; init; }

    /// <summary>"Hành lý" — VD "23kg".</summary>
    public string? Baggage { get; init; }

    public static readonly IReadOnlyList<string> DayTypes = ["Ngày thường", "Cuối tuần", "Ngày lễ"];

    public static readonly IReadOnlyList<string> TicketTypes = ["Vé series", "Vé lẻ", "ADHOC"];

    /// <summary>Loại NCC nào có cột riêng nào ở dòng giá.</summary>
    public static bool CoCotKhachSan(ProviderType t) => t == ProviderType.Hotel;

    public static bool CoCotVeMayBay(ProviderType t) => t == ProviderType.Airline;

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static ProviderServiceLineProfile Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ProviderServiceLineProfile();
        }

        try
        {
            return JsonSerializer.Deserialize<ProviderServiceLineProfile>(json, Options) ?? new ProviderServiceLineProfile();
        }
        catch (JsonException)
        {
            return new ProviderServiceLineProfile();
        }
    }

    /// <summary>Serialize; null nếu rỗng hoàn toàn (không lưu JSON thừa).</summary>
    public string? ToJsonOrNull()
    {
        var empty = PeriodFrom is null && PeriodTo is null && DayType is null &&
            NetCostPerDay is null && SellPricePerDay is null && TicketType is null &&
            Route is null && DepartTime is null && ReturnTime is null &&
            DepositDeadline is null && Baggage is null;
        return empty ? null : JsonSerializer.Serialize(this, Options);
    }

    /// <summary>
    /// Bỏ trường không thuộc loại NCC đang chọn — đổi khách sạn sang vé máy bay rồi lưu thì giai
    /// đoạn/loại ngày phải mất theo, không nằm lại làm rác vô hình.
    /// </summary>
    public ProviderServiceLineProfile ChiGiuCuaLoai(ProviderType loai)
    {
        var ks = CoCotKhachSan(loai);
        var ve = CoCotVeMayBay(loai);

        return new ProviderServiceLineProfile
        {
            PeriodFrom = ks ? PeriodFrom : null,
            PeriodTo = ks ? PeriodTo : null,
            DayType = ks ? DayType : null,
            NetCostPerDay = ks ? NetCostPerDay : null,
            SellPricePerDay = ks ? SellPricePerDay : null,
            TicketType = ve ? TicketType : null,
            Route = ve ? Route : null,
            DepartTime = ve ? DepartTime : null,
            ReturnTime = ve ? ReturnTime : null,
            DepositDeadline = ve ? DepositDeadline : null,
            Baggage = ve ? Baggage : null,
        };
    }
}
