namespace TourKit.Shared.Application;

/// <summary>Trang kết quả cho endpoint list (bắt buộc phân trang — không trả full bảng).</summary>
public sealed record Paged<T>(IReadOnlyList<T> Items, int Total, int Page, int Size);

/// <summary>Tham số phân trang chuẩn (clamp an toàn).</summary>
public sealed record PageQuery(int Page = 1, int Size = 20)
{
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafeSize => Size is < 1 or > 200 ? 20 : Size;
    public int Skip => (SafePage - 1) * SafeSize;
}
