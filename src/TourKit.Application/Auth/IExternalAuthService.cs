namespace TourKit.Application.Auth;

public interface IExternalAuthService
{
    Task<ExternalAuthOutcome> ResolveAsync(
        ExternalIdentity identity,
        CancellationToken ct = default);
}
