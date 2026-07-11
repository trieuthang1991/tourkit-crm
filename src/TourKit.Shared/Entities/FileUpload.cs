namespace TourKit.Shared.Entities;

/// <summary>
/// Tệp đính kèm (legacy file_upload/FileUpload*) — metadata; nội dung nằm ở <c>IFileStorage</c>
/// (local dev → S3/Azure prod, conventions §8). StorageKey do server sinh (guid), không tin path client.
/// </summary>
public sealed class FileUpload : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = string.Empty;     // tên gốc client (chỉ để hiển thị/tải về)
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StorageKey { get; set; } = string.Empty;   // khoá lưu trữ do server sinh
}
