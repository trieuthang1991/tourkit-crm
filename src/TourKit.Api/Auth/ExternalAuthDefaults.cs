namespace TourKit.Api.Auth;

/// <summary>
/// Cookie tạm dùng riêng cho chặng "đi vòng qua nhà cung cấp ngoài" (Google).
///
/// Vì sao KHÔNG dùng luôn cookie đăng nhập chính: lúc Google trả người dùng về, ta mới chỉ biết
/// "đây là email đã xác minh của Google", chưa biết email đó có thuộc tài khoản nào trong hệ thống
/// không — người mới hoàn toàn thì còn phải khai tên công ty đã. Nếu ký thẳng vào cookie chính ở
/// bước đó thì có một khoảng thời gian người chưa thuộc công ty nào vẫn mang cookie hợp lệ đi lại
/// trong ứng dụng. Tách ra cookie riêng, sống 10 phút, chỉ đủ để đi hết luồng rồi bị xoá.
/// </summary>
public static class ExternalAuthDefaults
{
    /// <summary>Tên scheme của cookie tạm.</summary>
    public const string Scheme = "tourkit_external";

    /// <summary>Thời gian sống: đủ cho người dùng điền form đăng ký công ty, không hơn.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
}
