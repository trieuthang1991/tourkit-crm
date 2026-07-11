using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;
using TourKit.Shared.Tenancy;

namespace TourKit.Infrastructure.Persistence;

/// <summary>
/// Ghi <see cref="ActivityLog"/> tự động cho mọi thao tác ghi entity nghiệp vụ (Insert/Update/Delete).
/// Chỉ ghi khi request có tenant (bỏ qua seed/system op). Bỏ qua chính ActivityLog để tránh đệ quy.
/// Chạy ở SavingChanges — sau khi AppDbContext đã gán TenantId/timestamp nên tự set đủ field cho log.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ITenantContext tenant, ICurrentUserContext user)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null || !tenant.HasTenant)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var logs = new List<ActivityLog>();

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity is ActivityLog)
            {
                continue; // không audit chính bản ghi audit
            }

            var action = entry.State switch
            {
                EntityState.Added => "Insert",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => (string?)null,
            };
            if (action is null)
            {
                continue;
            }

            logs.Add(new ActivityLog
            {
                TenantId = tenant.TenantId,
                UserId = user.UserId,
                Action = action,
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                Changes = BuildChanges(entry),
                CreatedAt = now,
            });
        }

        // Thêm sau vòng lặp để không audit chính các log vừa tạo.
        foreach (var log in logs)
        {
            context.Add(log);
        }
    }

    private static string? BuildChanges(EntityEntry entry)
    {
        if (entry.State == EntityState.Deleted)
        {
            return null;
        }

        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (entry.State == EntityState.Added)
            {
                dict[prop.Metadata.Name] = prop.CurrentValue;
            }
            else if (entry.State == EntityState.Modified && prop.IsModified)
            {
                dict[prop.Metadata.Name] = new { old = prop.OriginalValue, @new = prop.CurrentValue };
            }
        }

        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict);
    }
}
