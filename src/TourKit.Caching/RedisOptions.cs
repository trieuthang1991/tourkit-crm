namespace TourKit.Caching;

/// <summary>Cấu hình tầng cache dùng chung (section <c>Redis</c>).</summary>
public sealed class RedisOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Redis";

    /// <summary>
    /// Chuỗi kết nối Redis. Để TRỐNG thì cache chỉ còn tầng trong bộ nhớ tiến trình và ứng dụng vẫn
    /// chạy bình thường. Nhận cả dạng "ENC:" (Crypton) — tự giải khi dựng client.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Tiền tố khoá, để nhiều ứng dụng dùng chung một máy chủ Redis không đụng nhau.</summary>
    public string InstanceName { get; set; } = "tourkit:";
}
