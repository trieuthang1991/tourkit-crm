namespace TourKit.Application.Files;

/// <summary>
/// Trừu tượng lưu trữ tệp (conventions §8): local ở dev → S3/Azure Blob ở prod, đổi bằng cấu hình.
/// StorageKey do implementation sinh (server-side, không tin path client).
/// </summary>
public interface IFileStorage
{
    /// <summary>Lưu nội dung, trả về storageKey để đọc lại sau.</summary>
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken ct = default);

    /// <summary>Mở stream đọc theo storageKey; null nếu không tồn tại.</summary>
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default);
}
