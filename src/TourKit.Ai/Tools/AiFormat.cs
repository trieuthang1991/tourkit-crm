using System.Globalization;

namespace TourKit.Ai.Tools;

/// <summary>Định dạng số cho phần văn bản mà model đọc.</summary>
internal static class AiFormat
{
    /// <summary>
    /// Tiền kiểu Việt Nam (dấu chấm ngăn nghìn). Đi từ InvariantCulture rồi đổi dấu, không lấy culture
    /// "vi-VN" — máy chạy có thể bật chế độ bất biến toàn cục và khi đó culture đó im lặng trở thành
    /// Invariant, làm số hiện ra sai kiểu.
    /// </summary>
    public static string Money(decimal value) =>
        value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');

    /// <summary>Tỉ lệ dạng phân số 0..1 thành phần trăm một chữ số thập phân.</summary>
    public static string Percent(decimal ratio) =>
        (ratio * 100m).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    /// <summary>Số nguyên có ngăn nghìn.</summary>
    public static string Count(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');
}
