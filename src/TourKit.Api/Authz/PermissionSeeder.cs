using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Authz;

/// <summary>Upsert catalog quyền vào bảng global Permission (idempotent). Chạy khi startup.</summary>
public static class PermissionSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var existing = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        var missing = Permissions.All.Where(c => !existing.Contains(c.Code));
        foreach (var (code, group) in missing)
        {
            db.Permissions.Add(new Permission { Code = code, Group = group });
        }

        await db.SaveChangesAsync(ct);
    }
}
