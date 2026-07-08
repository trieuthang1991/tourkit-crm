namespace TourKit.Api.Provisioning;

public sealed record RegisterTenantRequest(
    string CompanyName, string Slug, string AdminEmail, string AdminPassword, string AdminFullName);

public sealed record RegistrationResponse(Guid TenantId, string Slug, Guid AdminUserId);
