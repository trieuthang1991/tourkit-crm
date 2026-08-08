using Microsoft.Extensions.AI;

namespace TourKit.Ai.Abstractions;

/// <summary>
/// ADAPTER: biết cách dựng một <see cref="IChatClient"/> cho MỘT kiểu giao thức.
///
/// Đây là chỗ duy nhất trong hệ thống biết tên một hãng cụ thể. Thêm hãng mới = thêm một project nhỏ
/// cài interface này, không sửa lõi và không kéo SDK lạ vào các assembly khác. Cấu hình chọn adapter
/// nào qua <c>Ai:Providers:&lt;tên&gt;:Kind</c>.
///
/// Từ đây trở lên, mọi thứ chỉ nói chuyện qua <see cref="IChatClient"/> — hợp đồng trung lập của .NET,
/// nên đổi từ DeepSeek sang Claude không đụng một dòng nào ở lõi hay ở giao diện.
/// </summary>
public interface IChatClientProvider
{
    /// <summary>Khớp với <c>Ai:Providers:&lt;tên&gt;:Kind</c>. So sánh không phân biệt hoa thường.</summary>
    string Kind { get; }

    /// <summary>Dựng client cho một nhà cung cấp + model cụ thể.</summary>
    IChatClient Create(AiProviderOptions provider, string model);
}
