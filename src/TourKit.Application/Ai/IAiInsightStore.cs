namespace TourKit.Application.Ai;

/// <summary>Một lần AI làm việc trên bản ghi, đã lưu lại.</summary>
public sealed record AiInsightDto(
    Guid Id, string Kind, DateTimeOffset CreatedAt, Guid UserId, string? UserName,
    string? Text, int? Score, string? Band, string? Summary, string? DetailJson);

/// <summary>Dữ liệu để lưu một lần chạy.</summary>
public sealed record SaveAiInsightDto(
    string EntityName, string EntityId, string Kind, Guid UserId,
    string? Text = null, int? Score = null, string? Band = null,
    string? Summary = null, string? DetailJson = null);

/// <summary>
/// Kho kết quả AI theo từng bản ghi.
///
/// Phân quyền KHÔNG nằm ở đây mà ở tầng API (AiRecordAccess) — cùng lý do như bình luận: chỉ có
/// tầng đó biết người đang đăng nhập có được xem bản ghi gốc hay không, và nhận định thì kể lại
/// chính nội dung bản ghi.
/// </summary>
public interface IAiInsightStore
{
    /// <summary>Lưu một lần chạy. Luôn THÊM DÒNG MỚI, không ghi đè — lịch sử là mục đích của bảng này.</summary>
    Task<Guid> SaveAsync(SaveAiInsightDto dto, CancellationToken ct = default);

    /// <summary>Kết quả mới nhất của mỗi loại việc, để màn hình hiện ngay khi mở hồ sơ.</summary>
    Task<IReadOnlyList<AiInsightDto>> LatestAsync(string entityName, string entityId, CancellationToken ct = default);

    /// <summary>Lịch sử một loại việc, mới nhất trước. Giới hạn số dòng để màn hình không kéo vô hạn.</summary>
    Task<IReadOnlyList<AiInsightDto>> HistoryAsync(string entityName, string entityId, string kind, int take = 20, CancellationToken ct = default);
}
