namespace TourKit.Api.BackgroundJobs;

/// <summary>
/// Job nền demo — chứng minh hạ tầng Hangfire (conventions §8) chạy. Job nghiệp vụ thật
/// (nhắc hạn giữ chỗ, gửi chăm sóc tự động...) thêm sau khi chốt với chủ dự án — nhiều job cần
/// tích hợp email (§4.2.9) và cách chạy job đa-tenant (dùng IgnoreQueryFilters lặp theo tenant).
/// </summary>
public sealed class HeartbeatJob(ILogger<HeartbeatJob> logger)
{
    public void Run() => logger.LogInformation("Hangfire background jobs alive.");
}
