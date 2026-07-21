namespace TourKit.Caching;

/// <summary>
/// Cổng cache của ứng dụng. Nơi gọi chỉ thấy interface này, KHÔNG biết bên dưới là Redis hay
/// bộ nhớ tiến trình — đổi hạ tầng cache không phải sửa code nghiệp vụ.
/// </summary>
public interface ITkCache
{
    /// <summary>
    /// Lấy theo khoá; chưa có (hoặc cache trục trặc) thì gọi <paramref name="factory"/> lấy dữ liệu
    /// thật rồi ghi lại cache. Cache hỏng KHÔNG bao giờ làm hỏng lời gọi — luôn trả về dữ liệu thật.
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory);

    Task RemoveAsync(string key);
}
