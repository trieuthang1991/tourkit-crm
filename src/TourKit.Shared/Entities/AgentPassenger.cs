namespace TourKit.Shared.Entities;

/// <summary>Hành khách của một <see cref="AgentBooking"/> (B2B Portal §4.2.5 — Quản lý Hành khách).</summary>
public sealed class AgentPassenger : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AgentBookingId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? PassportNo { get; set; }
    public string? Nationality { get; set; }
    public string? Note { get; set; }
}
