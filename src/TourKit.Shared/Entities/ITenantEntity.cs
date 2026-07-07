namespace TourKit.Shared.Entities;

/// <summary>Đánh dấu entity thuộc về một tenant. Mọi bảng nghiệp vụ phải implement.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
