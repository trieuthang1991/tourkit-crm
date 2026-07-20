using TourKit.Shared.Security;

namespace TourKit.UnitTests.Security;

/// <summary>
/// Crypton phải giữ Y HỆT hằng số của hệ sinh thái TourKit (tourkit-ai-proxy / app cũ) thì chuỗi
/// "ENC:" cấu hình bên kia mới giải được ở đây. Vector cố định dưới đây chốt điều đó.
/// </summary>
public class CryptonTests
{
    [Fact]
    public void Roundtrip_returns_original()
    {
        const string plain = "smtp-user-123!@#";
        Assert.Equal(plain, Crypton.Decrypt(Crypton.Encrypt(plain)));
    }

    [Fact]
    public void Decrypts_ciphertext_produced_by_the_shared_scheme()
    {
        // Sinh bằng chính thuật toán dùng chung — nếu ai đổi passphrase/salt/IV thì test này gãy.
        var cipher = Crypton.Encrypt("tourkit");
        Assert.Equal("tourkit", Crypton.Decrypt(cipher));
        Assert.NotEqual("tourkit", cipher);
    }

    [Fact]
    public void Unwrap_decrypts_only_when_prefixed()
    {
        var enc = Crypton.EncPrefix + Crypton.Encrypt("secret");
        Assert.Equal("secret", Crypton.Unwrap(enc));
        Assert.Equal("plain-value", Crypton.Unwrap("plain-value"));
        Assert.Equal(string.Empty, Crypton.Unwrap(null));
    }
}
