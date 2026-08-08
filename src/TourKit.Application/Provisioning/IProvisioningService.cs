namespace TourKit.Application.Provisioning;

public enum RegistrationError { None, SlugTaken, EmailTaken, Conflict, Invalid }

public sealed record RegistrationOutcome(RegistrationError Error, RegistrationResponse? Response);

public interface IProvisioningService
{
    Task<RegistrationOutcome> RegisterAsync(RegisterTenantRequest req);

    /// <summary>Đăng ký công ty cho người vừa xác minh danh tính qua nhà cung cấp ngoài.</summary>
    Task<RegistrationOutcome> RegisterExternalAsync(RegisterExternalTenantRequest req);
}
