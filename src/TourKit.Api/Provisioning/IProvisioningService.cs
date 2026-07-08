namespace TourKit.Api.Provisioning;

public enum RegistrationError { None, SlugTaken, Invalid }

public sealed record RegistrationOutcome(RegistrationError Error, RegistrationResponse? Response);

public interface IProvisioningService
{
    Task<RegistrationOutcome> RegisterAsync(RegisterTenantRequest req, CancellationToken ct);
}
