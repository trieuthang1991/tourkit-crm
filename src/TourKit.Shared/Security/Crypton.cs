using System.Security.Cryptography;
using System.Text;

namespace TourKit.Shared.Security;

/// <summary>
/// AES-256/CBC — PORT NGUYÊN từ hệ sinh thái TourKit (tourkit-ai-proxy/Services/Security/Crypton.cs,
/// vốn port từ TourKit.Shared của app cũ). PHẢI giữ y hệt passPhrase/salt/IV/keySize/iterations thì
/// chuỗi "ENC:" mã hoá bên kia mới giải được ở đây. KHÔNG đổi các hằng số dưới đây.
///
/// CẢNH BÁO: key nằm cứng trong source → đây là CHE (obfuscation), KHÔNG phải bảo mật thật.
/// Ai đọc được repo là giải được. Với secret thật nên dùng biến môi trường / secret store.
/// </summary>
public static class Crypton
{
    public const string EncPrefix = "ENC:";

    private const string PassPhrase = "Pas5pr@se";
    private const string SaltValue = "s@1tValue";
    private const string InitVector = "@1B2c3D4e5F6g7H8"; // 16 bytes
    private const int KeySize = 256;
    private const int Iterations = 2;

    public static string Encrypt(string plainText) => EncryptCore(plainText, PassPhrase, SaltValue);

    public static string Decrypt(string cipherText) => DecryptCore(cipherText, PassPhrase, SaltValue);

    /// <summary>Giải giá trị cấu hình: có tiền tố "ENC:" thì giải mã, không thì trả nguyên văn.</summary>
    public static string Unwrap(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.StartsWith(EncPrefix, StringComparison.Ordinal)
            ? Decrypt(value[EncPrefix.Length..])
            : value;
    }

    private static string EncryptCore(string plainText, string passPhrase, string salt)
    {
        var ivBytes = Encoding.ASCII.GetBytes(InitVector);
        var saltBytes = Encoding.ASCII.GetBytes(salt);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        // SYSLIB0041/CA5373: PasswordDeriveBytes + SHA1/2 vòng là YẾU và đã lỗi thời. Cố ý giữ NGUYÊN
        // vì phải giải được chuỗi "ENC:" do hệ TourKit cũ/proxy sinh ra — đổi sang PBKDF2 là hỏng hết
        // cấu hình đang chạy. Đây chỉ là lớp CHE cấu hình, không dùng cho dữ liệu nhạy cảm mới.
#pragma warning disable SYSLIB0041, CA5373
        using var password = new PasswordDeriveBytes(passPhrase, saltBytes, "SHA1", Iterations);
        var keyBytes = password.GetBytes(KeySize / 8);
#pragma warning restore SYSLIB0041, CA5373

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(keyBytes, ivBytes);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plainBytes, 0, plainBytes.Length);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    private static string DecryptCore(string cipherText, string passPhrase, string salt)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        byte[] cipherBytes;
        try
        {
            cipherBytes = Convert.FromBase64String(cipherText);
        }
        catch (FormatException)
        {
            return string.Empty;
        }

        var ivBytes = Encoding.ASCII.GetBytes(InitVector);
        var saltBytes = Encoding.ASCII.GetBytes(salt);

        // Xem ghi chú ở EncryptCore: cố ý giữ thuật toán cũ để tương thích chuỗi "ENC:" hệ TourKit.
#pragma warning disable SYSLIB0041, CA5373
        using var password = new PasswordDeriveBytes(passPhrase, saltBytes, "SHA1", Iterations);
        var keyBytes = password.GetBytes(KeySize / 8);
#pragma warning restore SYSLIB0041, CA5373

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(keyBytes, ivBytes);
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        // AES-CBC + PKCS7 đọc theo block 16 bytes; CopyTo đảm bảo đọc hết block cuối (xử lý padding).
        using var output = new MemoryStream();
        cs.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
