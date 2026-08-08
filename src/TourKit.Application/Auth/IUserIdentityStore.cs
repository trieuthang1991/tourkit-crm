using TourKit.Shared.Entities;

namespace TourKit.Application.Auth;

public interface IUserIdentityStore
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task<UserExternalLogin?> FindExternalAsync(string provider, string subject, CancellationToken ct = default);
    Task<bool> HasExternalAsync(Guid userId, string provider, CancellationToken ct = default);
    Task AddExternalAsync(UserExternalLogin login, CancellationToken ct = default);
}
