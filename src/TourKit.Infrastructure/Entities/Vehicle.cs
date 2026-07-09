using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Xe (legacy vehicle): tên xe, hãng, loại ghế/số chỗ.</summary>
public sealed class Vehicle : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;   // name
    public string? FirmName { get; set; }              // nameFirm
    public int SeatType { get; set; }                  // typeSeat (4/7/16/29/45...)
    public int Status { get; set; }
}
