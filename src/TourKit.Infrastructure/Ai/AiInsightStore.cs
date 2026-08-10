using Microsoft.EntityFrameworkCore;
using TourKit.Application.Ai;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Ai;

/// <summary>
/// Đọc/ghi kết quả AI thẳng qua DbContext thay vì IRepository.
///
/// Lý do: mọi truy vấn ở đây đều cần SẮP XẾP rồi LẤY N DÒNG ĐẦU, mà IRepository.ListAsync
/// materialize ngay — dùng nó là nạp toàn bộ lịch sử của bản ghi về rồi mới cắt trong bộ nhớ. Hồ sơ
/// chấm càng nhiều thì càng chậm, đúng chiều ngược với mong muốn.
/// </summary>
public sealed class AiInsightStore(AppDbContext db) : IAiInsightStore
{
    /// <summary>Ba loại việc AI làm. Cố định ở đây để LatestAsync chỉ cần đúng ba truy vấn chỉ mục.</summary>
    private static readonly string[] CacLoai = ["Review", "Summary", "Draft"];

    public async Task<Guid> SaveAsync(SaveAiInsightDto dto, CancellationToken ct = default)
    {
        var row = new AiInsight
        {
            EntityName = dto.EntityName,
            EntityId = dto.EntityId,
            Kind = dto.Kind,
            UserId = dto.UserId,
            Text = dto.Text,
            Score = dto.Score,
            Band = dto.Band,
            Summary = dto.Summary,
            DetailJson = dto.DetailJson,
        };

        db.AiInsights.Add(row);
        await db.SaveChangesAsync(ct);
        return row.Id;
    }

    public async Task<IReadOnlyList<AiInsightDto>> LatestAsync(string entityName, string entityId, CancellationToken ct = default)
    {
        // Ba truy vấn một dòng, mỗi cái đi thẳng vào chỉ mục (TenantId, EntityName, EntityId, Kind,
        // CreatedAt). Gộp thành một câu GroupBy trông gọn hơn nhưng phụ thuộc vào việc EF dịch được
        // "lấy dòng đầu của mỗi nhóm" — dịch không ra thì nổ lúc chạy, mà chỗ này nằm trên đường mở
        // hồ sơ nên hỏng là cả màn hình hỏng.
        var ra = new List<AiInsight>(CacLoai.Length);
        foreach (var loai in CacLoai)
        {
            var row = await db.AiInsights
                .Where(x => x.EntityName == entityName && x.EntityId == entityId && x.Kind == loai)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (row is not null) { ra.Add(row); }
        }

        return await GanTenNguoiAsync(ra, ct);
    }

    public async Task<IReadOnlyList<AiInsightDto>> HistoryAsync(string entityName, string entityId, string kind, int take = 20, CancellationToken ct = default)
    {
        var rows = await db.AiInsights
            .Where(x => x.EntityName == entityName && x.EntityId == entityId && x.Kind == kind)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        return await GanTenNguoiAsync(rows, ct);
    }

    /// <summary>
    /// Lấy tên người bấm theo LÔ. Tra từng dòng một là N+1 truy vấn cho một danh sách mà người dùng
    /// chỉ liếc qua xem ai đã chấm.
    /// </summary>
    private async Task<IReadOnlyList<AiInsightDto>> GanTenNguoiAsync(List<AiInsight> rows, CancellationToken ct)
    {
        if (rows.Count == 0) { return []; }

        var ids = rows.Select(r => r.UserId).Distinct().ToList();
        var ten = await db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return rows.Select(r => new AiInsightDto(
            r.Id, r.Kind, r.CreatedAt, r.UserId, ten.GetValueOrDefault(r.UserId),
            r.Text, r.Score, r.Band, r.Summary, r.DetailJson)).ToList();
    }
}
