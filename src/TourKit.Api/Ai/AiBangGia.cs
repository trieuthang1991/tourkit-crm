using TourKit.Ai;
using TourKit.Ai.Abstractions;
using TourKit.Application.Providers.Import;

namespace TourKit.Api.Ai;

/// <summary>
/// Đọc bảng giá từ tài liệu báo giá (PDF/DOC/DOCX) bằng AI, rồi trả về BẢNG đúng mẫu để lớp kiểm tất
/// định xử lý tiếp.
///
/// Chia việc rất rõ: model chỉ khớp chữ trong tài liệu vào tên cột cho sẵn; mọi luật "số này hợp lệ
/// không, ngày này parse được không, giai đoạn có ngược không" vẫn do
/// <see cref="NhapDichVuService"/> làm, y hệt đường Excel/CSV. Model đoán sai thì hỏng đúng một dòng
/// và có lý do rõ ràng ở bảng xem trước, chứ không đẩy được số rác vào bảng giá.
/// </summary>
public sealed class AiBangGia(
    AiChatClientFactory factory,
    AiUsageGuard usage,
    ILoggerFactory loggers)
{
    /// <summary>Đọc tài liệu bằng AI có đang dùng được không.</summary>
    public bool CoDungDuoc => factory.For(AiFeatures.DocumentRead) is not null;

    /// <summary>
    /// Bóc <paramref name="vanBan"/> thành bảng theo <paramref name="cot"/>.
    /// Trả về <c>null</c> kèm lý do khi không dùng được.
    /// </summary>
    public async Task<(BangNhap? Bang, string? Loi)> DocAsync(
        string vanBan, IReadOnlyList<string> cot, Guid userId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cot);

        var resolved = factory.For(AiFeatures.DocumentRead);
        if (resolved is null)
        {
            return (null, $"Tính năng \"{AiFeatures.Label(AiFeatures.DocumentRead)}\" đang tắt. "
                + $"Bật Ai:Features:{AiFeatures.DocumentRead} trong cấu hình để nhập từ tài liệu.");
        }

        if (string.IsNullOrWhiteSpace(vanBan))
        {
            // Ảnh chụp/PDF scan không có lớp chữ thì bóc ra rỗng. Nói thẳng lý do, đừng gửi chuỗi
            // rỗng cho model rồi trả về "không đọc được gì" mà người dùng không hiểu vì sao.
            return (null, "Không đọc được chữ nào trong tài liệu. Nếu là bản scan, hãy dùng tệp gốc hoặc mẫu Excel/CSV.");
        }

        if (usage.Reject(userId) is { } refusal)
        {
            return (null, refusal);
        }

        var (client, config) = resolved.Value;
        var settings = new AiChatSettings(
            config.Model,
            config.Settings.MaxOutputTokens,
            config.Settings.MaxToolRounds,
            config.Settings.Temperature);

        var svc = new AiTableExtractService(client, settings, loggers.CreateLogger<AiTableExtractService>());
        var rows = await svc.ExtractAsync(vanBan, cot, ct).ConfigureAwait(false);

        // Không đi qua vòng lặp công cụ nên không có số token theo lượt; trừ tạm theo độ dài tài liệu.
        usage.Record(userId, vanBan.Length / 3);

        if (rows.Count == 0)
        {
            return (null, "Không tìm thấy bảng giá nào trong tài liệu. Hãy kiểm tra lại tệp hoặc dùng mẫu Excel/CSV.");
        }

        return (ThanhBang(rows, cot), null);
    }

    /// <summary>
    /// Ghép các dòng model trả về thành bảng theo THỨ TỰ CỘT của mẫu.
    ///
    /// Đi theo danh sách cột thay vì theo key model trả về: model có thể bỏ sót cột, đảo thứ tự, hoặc
    /// bịa thêm key. Cột thiếu thành ô rỗng, key lạ bị bỏ — bảng luôn đúng hình dạng mà lớp kiểm mong đợi.
    /// </summary>
    public static BangNhap ThanhBang(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows, IReadOnlyList<string> cot)
    {
        var dong = rows
            .Select(r => (IReadOnlyList<string>)cot.Select(c => r.TryGetValue(c, out var v) ? v : "").ToList())
            .ToList();

        return new BangNhap(cot, dong);
    }
}
