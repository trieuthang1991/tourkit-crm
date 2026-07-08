using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Catalog.Features;

// ---- VERTICAL SLICE MẪU: liệt kê mẫu tour có PHÂN TRANG (Query + Handler) ----

public sealed record ListTourTemplatesQuery(int Page, int Size) : IQuery<Paged<TourTemplateResponse>>;

public sealed class ListTourTemplatesHandler : IQueryHandler<ListTourTemplatesQuery, Paged<TourTemplateResponse>>
{
    private readonly AppDbContext _db;

    public ListTourTemplatesHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<TourTemplateResponse>>> Handle(ListTourTemplatesQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.TourTemplates.AsNoTracking().OrderBy(t => t.Title);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(t => new TourTemplateResponse(
                t.Id, t.Code, t.Title, t.TourType, t.TotalSlots, t.ReservationHours,
                t.PriceAdult, t.PriceChild, t.PriceChildSmall, t.PriceBaby, t.TermsNote, t.Status))
            .ToListAsync(ct);

        return new Paged<TourTemplateResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
