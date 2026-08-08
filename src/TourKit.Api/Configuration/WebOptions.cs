namespace TourKit.Api.Configuration;

/// <summary>Nguồn được phép gọi API từ trình duyệt (section <c>Cors</c>).</summary>
public sealed class CorsOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Danh sách origin cho phép. Bỏ trống thì dùng cổng dev mặc định của Vite.
    /// KHÔNG bao giờ dùng dấu sao ở môi trường chạy thật — đó là mở API cho mọi tên miền trên Internet.
    /// </summary>
    public IList<string> Origins { get; } = [];

    /// <summary>Danh sách thật sự áp dụng — mặc định nằm ở đây, không nằm rải rác tại chỗ gọi.</summary>
    public string[] ResolveOrigins() =>
        Origins.Count > 0 ? [.. Origins] : ["http://localhost:5173", "http://localhost:4173"];
}

/// <summary>Công tắc chạy nền (section <c>BackgroundJobs</c>).</summary>
public sealed class BackgroundJobsOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "BackgroundJobs";

    /// <summary>
    /// Bật máy chủ job nền. Tắt khi chỉ muốn chạy web mà không muốn job tự khởi động (kiểm thử, một
    /// nút trong cụm chỉ phục vụ request).
    /// </summary>
    public bool Enabled { get; set; } = true;
}
