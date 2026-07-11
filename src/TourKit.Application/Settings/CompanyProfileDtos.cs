namespace TourKit.Application.Settings;

public sealed record CompanyProfileDto(
    string Name, string? ShortName, string? Address, string? Hotline, string? Email, string? Website,
    string? TaxCode, string? LegalRepName, string? LegalRepTitle, string? LicenseNumber, string? BankAccount);
