namespace TourKit.Infrastructure.Storage;

/// <summary>Cấu hình nơi cất tệp tải lên (section <c>FileStorage</c>).</summary>
public sealed class FileStorageOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Thư mục gốc khi lưu tệp ngay trên máy chủ. Bỏ trống thì dùng <see cref="ResolveLocalRoot"/>.
    /// </summary>
    public string LocalRoot { get; set; } = string.Empty;

    /// <summary>
    /// Thư mục thật sự sẽ dùng. Giá trị mặc định nằm ở ĐÂY chứ không nằm rải rác tại chỗ gọi — nơi
    /// thứ hai cần biết chỗ cất tệp (dọn tệp rác, sao lưu, màn quản trị) sẽ lấy đúng cùng một đường
    /// dẫn thay vì tự đoán lại.
    /// </summary>
    public string ResolveLocalRoot() =>
        string.IsNullOrWhiteSpace(LocalRoot)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads")
            : LocalRoot;
}
