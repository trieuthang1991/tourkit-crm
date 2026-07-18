using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Api.BackgroundJobs;

/// <summary>
/// Tự động NHẢ chỗ giữ hết hạn (P0-1): chỗ đang GIỮ (còn đếm ngược, chưa cọc, chưa huỷ) mà
/// <c>HoldExpiresAt</c> đã qua → nhả chỗ để giải phóng slot. Nhả = đặt <c>Status = 1</c> (huỷ,
/// giống <see cref="TourKit.Application.Booking.BookingService"/>.CancelSeatAsync) + xoá đếm ngược,
/// ghi một dòng <see cref="CancelSeat"/> làm dấu vết (KHÔNG hoàn tiền — chỗ giữ chưa cọc).
///
/// Đa-tenant theo pattern CareReminderJob/HoldReminderJob: quét bằng <c>IgnoreQueryFilters()</c> để
/// thấy mọi tenant, xử lý theo TỪNG tenant (set tenant hiện hành trước khi lưu để qua guard
/// chống-ghi-chéo-tenant trong <see cref="AppDbContext"/>), lưu riêng từng tenant.
/// </summary>
public sealed partial class HoldReleaseJob(
    AppDbContext db,
    AmbientTenantContext tenantScope,
    ILogger<HoldReleaseJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Chỗ giữ hết hạn: còn đếm ngược (HoldExpiresAt != null) đã qua hạn, chưa cọc, chưa huỷ.
        var expired = await db.Set<TourCustomer>()
            .IgnoreQueryFilters()
            .Where(s => s.HoldExpiresAt != null && s.HoldExpiresAt < now
                && s.UpfrontAmount == 0 && s.Status == 0)
            .ToListAsync(ct);

        if (expired.Count == 0)
        {
            return;
        }

        var released = 0;
        foreach (var group in expired.GroupBy(s => s.TenantId))
        {
            // Guard AppDbContext yêu cầu entity Modified/Added thuộc đúng tenant hiện hành → set trước khi lưu.
            tenantScope.SetTenant(group.Key);

            foreach (var seat in group)
            {
                // Dấu vết nhả chỗ (mirror CancelSeatAsync) — không hoàn tiền vì chỗ giữ chưa cọc (upfront = 0).
                db.Set<CancelSeat>().Add(new CancelSeat
                {
                    TourCustomerId = seat.Id,
                    OrderId = seat.OrderId,
                    Note = "Tự động nhả chỗ: hết hạn giữ chỗ.",
                    RefundAmount = 0m,
                    RefundRemain = RefundMath.Remain(seat.UpfrontAmount, 0m),
                    RefundPercentage = RefundMath.Percentage(seat.UpfrontAmount, 0m),
                });

                seat.Status = 1;            // statusCancel != 0 → đã huỷ → giải phóng slot
                seat.HoldExpiresAt = null;  // hết đếm ngược
                released++;
            }

            await db.SaveChangesAsync(ct); // lưu riêng từng tenant trước khi sang tenant khác
        }

        LogSummary(logger, expired.Count, released);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Hold releases: {Expired} expired, {Released} released.")]
    private static partial void LogSummary(ILogger logger, int expired, int released);
}
