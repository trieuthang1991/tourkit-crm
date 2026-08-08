using TourKit.Application.Auth;
using TourKit.Infrastructure.Tenancy;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Auth;

public sealed class ExternalAuthService(
    IUserIdentityStore identities,
    AmbientTenantContext tenant) : IExternalAuthService
{
    public async Task<ExternalAuthOutcome> ResolveAsync(
        ExternalIdentity identity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var provider = identity.Provider?.Trim();
        var subject = identity.Subject?.Trim();
        var email = identity.Email?.Trim();
        if (!identity.EmailVerified
            || string.IsNullOrWhiteSpace(provider)
            || string.IsNullOrWhiteSpace(subject)
            || string.IsNullOrWhiteSpace(email))
        {
            return Rejected("External identity requires a verified email, provider, and subject.");
        }

        var linkedLogin = await identities.FindExternalAsync(provider, subject, ct);
        if (linkedLogin is not null)
        {
            var linkedUser = await identities.FindByIdAsync(linkedLogin.UserId, ct);
            if (!await IsActiveAsync(linkedUser, ct))
            {
                return Rejected("The linked user or tenant is inactive.");
            }

            tenant.SetTenant(linkedUser!.TenantId);
            return new ExternalAuthOutcome(ExternalAuthStatus.Authenticated, linkedUser.Id);
        }

        var user = await identities.FindByEmailAsync(email, ct);
        if (user is null)
        {
            return new ExternalAuthOutcome(ExternalAuthStatus.NeedsOnboarding);
        }

        if (!await IsActiveAsync(user, ct))
        {
            return Rejected("The user or tenant is inactive.");
        }

        if (await identities.HasExternalAsync(user.Id, provider, ct))
        {
            return Rejected("This user is already linked to a different external subject.");
        }

        tenant.SetTenant(user.TenantId);
        await identities.AddExternalAsync(new UserExternalLogin
        {
            UserId = user.Id,
            Provider = provider,
            ProviderSubject = subject,
            ProviderEmail = email,
        }, ct);

        return new ExternalAuthOutcome(ExternalAuthStatus.Authenticated, user.Id);
    }

    private async Task<bool> IsActiveAsync(User? user, CancellationToken ct)
        => user is not null
           && user.IsActive
           && await identities.TenantIsActiveAsync(user.TenantId, ct);

    private static ExternalAuthOutcome Rejected(string error)
        => new(ExternalAuthStatus.Rejected, Error: error);
}
