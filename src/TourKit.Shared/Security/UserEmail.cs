namespace TourKit.Shared.Security;

public static class UserEmail
{
    public static string Normalize(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();
}
