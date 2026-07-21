using Microsoft.EntityFrameworkCore;
using TourKit.Application.Rooms;
using TourKit.Application.Rooms.Dtos;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Rooms;

/// <summary>
/// Hiện thực query Quỹ phòng — dùng <c>AppDbContext</c> trực tiếp (repo riêng theo convention §5).
/// Toàn bộ lọc/sắp/cắt trang/gộp chạy ở SQL.
/// </summary>
public sealed class RoomAllotmentQueries(AppDbContext db) : IRoomAllotmentQueries
{
    public async Task<(IReadOnlyList<RoomAllotment> Items, int Total)> PageAsync(
        RoomAllotmentListFilter filter, int page, int size)
    {
        var q = Build(filter);

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(a => a.ProviderRef).ThenBy(a => a.ServiceName).ThenBy(a => a.Date)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<RoomAllotmentStatsDto> StatsAsync(RoomAllotmentListFilter filter)
    {
        var q = Build(filter);

        // Một truy vấn gộp duy nhất: đếm ô, tổng tồn/đã đặt, và đếm theo từng loại ngày.
        // Bản cũ nạp cả 36.000 dòng về rồi chạy 9 phép đếm/cộng trong bộ nhớ.
        var agg = await q
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cells = g.Count(),
                TotalQuota = g.Sum(a => a.Quota),
                TotalBooked = g.Sum(a => a.Booked),
                Normal = g.Count(a => a.DayType == 0),
                Weekend = g.Count(a => a.DayType == 1),
                Holiday = g.Count(a => a.DayType == 2),
                Peak = g.Count(a => a.DayType == 3),
            })
            .FirstOrDefaultAsync();

        if (agg is null)
        {
            return new RoomAllotmentStatsDto(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        // "Số NCC" là COUNT(DISTINCT) — tách riêng vì gộp chung vào GroupBy trên không dịch được.
        var providers = await q.Select(a => a.ProviderRef).Distinct().CountAsync();

        // Tồn khả dụng phải là Σ max(0, quota − booked) theo TỪNG ô, không phải hiệu của hai tổng
        // (ô đã đặt vượt tồn sẽ làm lệch nếu trừ ở mức tổng).
        var available = await q.Select(a => a.Quota - a.Booked > 0 ? a.Quota - a.Booked : 0).SumAsync();

        return new RoomAllotmentStatsDto(
            agg.Cells, providers, agg.TotalQuota, agg.TotalBooked, available,
            agg.Normal, agg.Weekend, agg.Holiday, agg.Peak);
    }

    private IQueryable<RoomAllotment> Build(RoomAllotmentListFilter? filter)
    {
        var f = filter ?? new RoomAllotmentListFilter();
        var q = db.RoomAllotments.AsNoTracking().AsQueryable();

        if (f.ProviderRef is not null) { q = q.Where(a => a.ProviderRef == f.ProviderRef); }
        if (f.Province is not null) { q = q.Where(a => a.Province == f.Province); }
        if (f.Market is not null) { q = q.Where(a => a.Market == f.Market); }
        if (f.Rating is not null) { q = q.Where(a => a.Rating == f.Rating); }
        if (f.DateFrom is not null) { q = q.Where(a => a.Date >= f.DateFrom); }
        if (f.DateTo is not null) { q = q.Where(a => a.Date <= f.DateTo); }

        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            // ToLower().Contains dịch được trên CẢ Npgsql lẫn provider InMemory của test.
            // (EF.Functions.ILike nhanh hơn trên Postgres nhưng ném lỗi trên InMemory.)
            var kw = f.Q.Trim().ToLowerInvariant();

            // Đây là CÂY BIỂU THỨC EF, không phải code chạy trên chuỗi .NET: ToLower()/Contains ở đây
            // được DỊCH thành LOWER(...) LIKE '%...%' của SQL. Dùng StringComparison như analyzer
            // gợi ý sẽ khiến EF không dịch được và rơi về lọc trong bộ nhớ — đúng thứ đang cần bỏ.
#pragma warning disable CA1304, CA1311, CA1862
            q = q.Where(a =>
                a.ServiceName.ToLower().Contains(kw)
                || (a.ProjectName != null && a.ProjectName.ToLower().Contains(kw))
                || (a.Province != null && a.Province.ToLower().Contains(kw)));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return q;
    }
}
