namespace TourKit.Application.Provisioning;

public enum RegistrationError { None, SlugTaken, EmailTaken, Conflict, Invalid }

public sealed record RegistrationOutcome(RegistrationError Error, RegistrationResponse? Response);

public interface IProvisioningService
{
    Task<RegistrationOutcome> RegisterAsync(RegisterTenantRequest req);
}
