using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Application.Notifications;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;

namespace TourKit.Api.BackgroundJobs;

/// <summary>
/// Nhắc hạn giữ chỗ (roadmap Đợt 7): chỗ đang GIỮ (chưa cọc, chưa huỷ) sắp hết hạn trong
/// <see cref="ReminderWindowHours"/> giờ → email sales phụ trách đơn để chốt khách hoặc nhả chỗ,
/// đánh dấu <c>HoldReminderSentAt</c> để không nhắc lại. Đa-tenant theo pattern CareReminderJob:
/// quét IgnoreQueryFilters, lưu per-tenant (set tenant hiện hành trước SaveChanges).
/// </summary>
public sealed partial class HoldReminderJob(
    AppDbContext db,
    AmbientTenantContext tenantScope,
    IEmailSender email,
    ILogger<HoldReminderJob> logger)
{
    /// <summary>Nhắc khi còn ≤ 24h trước hạn giữ (chuẩn vận hành — đủ thời gian gọi khách chốt cọc).</summary>
    public const int ReminderWindowHours = 24;

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowEnd = now.AddHours(ReminderWindowHours);

        // Chỗ đang giữ: còn đếm ngược (HoldExpiresAt != null), chưa cọc, chưa huỷ, sắp hết hạn, chưa nhắc.
        var due = await db.Set<TourCustomer>()
            .IgnoreQueryFilters()
            .Where(s => s.HoldExpiresAt != null && s.HoldExpiresAt <= windowEnd
                && s.HoldReminderSentAt == null && s.UpfrontAmount == 0 && s.Status == 0)
            .ToListAsync(ct);

        if (due.Count == 0)
        {
            return;
        }

        // Người nhận = sales phụ trách đơn của chỗ giữ.
        var orderIds = due.Select(s => s.OrderId).Distinct().ToList();
        var orders = await db.Set<Order>()
            .IgnoreQueryFilters()
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, ct);
        var salesIds = orders.Values.Where(o => o.SalesUserId != null).Select(o => o.SalesUserId!.Value).Distinct().ToList();
        var users = await db.Set<User>()
            .IgnoreQueryFilters()
            .Where(u => salesIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var sent = 0;
        foreach (var group in due.GroupBy(s => s.TenantId))
        {
            tenantScope.SetTenant(group.Key); // qua guard chống-ghi-chéo-tenant khi lưu
            var markedInGroup = false;

            foreach (var seat in group)
            {
                if (!orders.TryGetValue(seat.OrderId, out var order)
                    || order.SalesUserId is not { } salesId
                    || !users.TryGetValue(salesId, out var sales)
                    || !sales.IsActive || string.IsNullOrWhiteSpace(sales.Email))
                {
                    continue; // chưa gán sales/không có email → để lần chạy sau (không đánh dấu)
                }

                var body = $"Chỗ giữ {seat.ReservationCode} (đơn {order.Code}) hết hạn lúc "
                    + $"{seat.HoldExpiresAt:yyyy-MM-dd HH:mm}. Liên hệ khách chốt cọc hoặc nhả chỗ.";
                await email.SendAsync(sales.Email, $"[Nhắc giữ chỗ] {order.Code}", body, ct);

                seat.HoldReminderSentAt = now;
                markedInGroup = true;
                sent++;
            }

            if (markedInGroup)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        LogSummary(logger, due.Count, sent);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Hold reminders: {Due} due, {Sent} sent.")]
    private static partial void LogSummary(ILogger logger, int due, int sent);
}
