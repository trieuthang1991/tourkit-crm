
using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>Khách tiềm năng (phễu bán). Convert thành Customer khi "Won".</summary>
public sealed class Lead : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Source { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public Guid? AssignedToUserId { get; set; }
    public Guid? BranchId { get; set; }             // Chi nhánh (legacy ChiNhanh)
    public Guid? ConvertedCustomerId { get; set; }
}
