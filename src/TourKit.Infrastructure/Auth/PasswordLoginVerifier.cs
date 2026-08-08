using TourKit.Application.Auth;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Auth;

internal static class PasswordLoginVerifier
{
    // Generated once from a random, non-account input. Keeping a valid Identity V3 hash here makes
    // unknown/deleted identities perform the same PBKDF2 work without generating a hash per request.
    private const string DummyPasswordHash =
        "AQAAAAIAAYagAAAAEIlt27g6lpjGGntms9PAVsGOQNFKsJSLusyNADO9hLgavN/zj1BDJ+fU2wclTYHchA==";

    public static bool Verify(IPasswordHasher hasher, User? user, string suppliedPassword)
        => hasher.Verify(user?.PasswordHash ?? DummyPasswordHash, suppliedPassword);
}
