using TourKit.Api.Auth;

namespace TourKit.Tests.Auth;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_then_verify_true()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("S3cret!");

        Assert.NotEqual("S3cret!", hash);      // không lưu plain
        Assert.True(hasher.Verify(hash, "S3cret!"));
    }

    [Fact]
    public void Verify_wrong_password_false()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("S3cret!");

        Assert.False(hasher.Verify(hash, "wrong"));
    }
}
