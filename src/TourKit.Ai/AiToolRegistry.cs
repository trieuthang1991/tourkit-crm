using TourKit.Ai.Abstractions;

namespace TourKit.Ai;

/// <summary>
/// Danh mục công cụ và bộ lọc quyền. Phân quyền của trợ lý = LỌC DANH SÁCH CÔNG CỤ đưa cho model,
/// không phải câu dặn dò trong prompt: kế toán không thấy công cụ giá vốn thì không có cách nào gọi
/// nó, kể cả khi người dùng cố dụ model.
/// </summary>
public sealed class AiToolRegistry(IEnumerable<IAiTool> tools)
{
    private readonly IReadOnlyList<IAiTool> _all = [.. tools];

    /// <summary>Toàn bộ công cụ đã đăng ký — chỉ dùng để kiểm kê, KHÔNG dùng để gửi cho model.</summary>
    public IReadOnlyList<IAiTool> All => _all;

    /// <summary>Danh sách công cụ người dùng với bộ quyền <paramref name="perms"/> được phép thấy.</summary>
    public IReadOnlyList<IAiTool> For(IReadOnlySet<string> perms)
    {
        ArgumentNullException.ThrowIfNull(perms);
        return [.. _all.Where(t => t.RequiredPermission is null || perms.Contains(t.RequiredPermission))];
    }

    /// <summary>
    /// Tra công cụ theo tên NHƯNG chỉ trong danh sách đã lọc — không bao giờ tra trên toàn bộ danh mục.
    /// Model bịa tên một công cụ ngoài quyền thì hàm này trả <c>null</c>.
    /// </summary>
    public static IAiTool? Find(IReadOnlyList<IAiTool> allowed, string name)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        return allowed.FirstOrDefault(t => string.Equals(t.Function.Name, name, StringComparison.Ordinal));
    }
}
