using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Application.Notifications;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;

namespace TourKit.Api.BackgroundJobs;

/// <summary>
/// Gửi chăm sóc tự động (roadmap Đợt 7): mỗi lịch chăm sóc (<see cref="CustomerCare"/>) tới hạn nhắc
/// (<c>RemindAt</c>) mà chưa gửi, email nhắc cho người phụ trách rồi đánh dấu <c>ReminderSentAt</c>
/// để không gửi lại. Dev dùng LogEmailSender (không cần SMTP); prod đổi Email:Provider=Smtp là gửi thật.
///
/// Đa-tenant: job không có tenant ambient nên (1) quét bằng <c>IgnoreQueryFilters()</c> để thấy mọi
/// tenant, (2) xử lý theo TỪNG tenant — set tenant hiện hành = tenant của nhóm rồi lưu, để qua được
/// guard chống-ghi-chéo-tenant trong <see cref="AppDbContext"/>. Lưu xong nhóm này mới sang nhóm khác.
/// </summary>
public sealed partial class CareReminderJob(
    AppDbContext db,
    AmbientTenantContext tenantScope,
    IEmailSender email,
    ILogger<CareReminderJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var due = await db.Set<CustomerCare>()
            .IgnoreQueryFilters()
            .Where(c => c.RemindAt != null && c.RemindAt <= now
                && c.ReminderSentAt == null && c.AssignedToUserId != null)
            .ToListAsync(ct);

        if (due.Count == 0)
        {
            return;
        }

        var userIds = due.Select(c => c.AssignedToUserId!.Value).Distinct().ToList();
        var users = await db.Set<User>()
            .IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var sent = 0;
        foreach (var group in due.GroupBy(c => c.TenantId))
        {
            // Guard AppDbContext yêu cầu entity Modified thuộc đúng tenant hiện hành → set trước khi lưu.
            tenantScope.SetTenant(group.Key);
            var markedInGroup = false;

            foreach (var care in group)
            {
                if (!users.TryGetValue(care.AssignedToUserId!.Value, out var user)
                    || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
                {
                    continue; // không có người nhận hợp lệ → để lần chạy sau (không đánh dấu đã gửi)
                }

                var body = $"Lịch chăm sóc \"{care.Title}\" tới hạn nhắc lúc {care.RemindAt:yyyy-MM-dd HH:mm}."
                    + (string.IsNullOrWhiteSpace(care.Detail) ? "" : $"\nNội dung: {care.Detail}");
                await email.SendAsync(user.Email, $"[Nhắc CSKH] {care.Title}", body, ct);

                care.ReminderSentAt = now;
                markedInGroup = true;
                sent++;
            }

            if (markedInGroup)
            {
                await db.SaveChangesAsync(ct); // lưu riêng từng tenant trước khi sang tenant khác
            }
        }

        LogSummary(logger, due.Count, sent);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Care reminders: {Due} due, {Sent} sent.")]
    private static partial void LogSummary(ILogger logger, int due, int sent);
}
