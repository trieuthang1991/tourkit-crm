using Microsoft.AspNetCore.Identity;
using TourKit.Shared.Entities;

namespace TourKit.Api.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(new User(), password);

    public bool Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(new User(), hash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
