namespace TourKit.Shared.Entities;

public sealed class UserExternalLogin : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderSubject { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
}
