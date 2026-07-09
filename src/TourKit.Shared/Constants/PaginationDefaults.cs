namespace TourKit.Shared.Constants;

/// <summary>
/// Hằng số phân trang dùng chung (một chỗ) — đừng rải magic number 20/200 khắp nơi.
/// Dùng ở Repository.PageAsync, controller mặc định, v.v.
/// </summary>
public static class PaginationDefaults
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;
    public const int FirstPage = 1;
}
