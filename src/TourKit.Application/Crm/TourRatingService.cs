using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Crm;

/// <summary>Đánh giá sau tour (legacy Rate). Stars 1..5, validate ở Create/Update.</summary>
public sealed class TourRatingService(
    IRepository<TourRating> repo,
    IValidator<CreateTourRatingDto> createValidator,
    IValidator<UpdateTourRatingDto> updateValidator) : ITourRatingService
{
    public async Task<PagedResult<TourRatingDto>> ListAsync(int page, int size, string? q = null, int? stars = null, int? status = null,
        Guid? salesUserId = null, Guid? operatorUserId = null)
    {
        var kw = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        System.Linq.Expressions.Expression<Func<TourRating, bool>> pred = r =>
            (stars == null || r.Stars == stars) &&
            (status == null || r.Status == status) &&
            (salesUserId == null || r.SalesUserId == salesUserId) &&
            (operatorUserId == null || r.OperatorUserId == operatorUserId);

        // Số sao/trạng thái/NVPT/NVĐH đẩy xuống SQL. Không từ khoá → cắt trang NGAY Ở SQL (CreatedAt giảm dần).
        if (kw == null)
        {
            var (items, total) = await repo.PageAsync(page, size, pred);
            return new PagedResult<TourRatingDto>(items.Select(Map).ToList(), total, page, size);
        }

        // Có từ khoá: lọc cột ở SQL trước, rồi so khớp text không dấu ở bộ nhớ (EF không dịch StringComparison).
        var all = await repo.ListAsync(pred);
        bool MatchQ(TourRating r) =>
            (r.CustomerName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.CustomerPhone?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.Comment?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);

        var filtered = all.Where(MatchQ).OrderByDescending(r => r.CreatedAt).ToList();
        var dtos = filtered.Skip((page - 1) * size).Take(size).Select(Map).ToList();
        return new PagedResult<TourRatingDto>(dtos, filtered.Count, page, size);
    }

    public async Task<TourRatingStatsDto> GetStatsAsync()
    {
        // Đếm theo bậc sao bằng MỘT câu GROUP BY ở SQL — không nạp cả bảng.
        var byStars = await repo.CountByAsync(r => r.Stars);
        int N(int s) => byStars.GetValueOrDefault(s);
        var total = byStars.Values.Sum();
        var sumStars = byStars.Sum(kv => kv.Key * kv.Value);
        var avg = total > 0 ? Math.Round((double)sumStars / total, 1) : 0;
        return new TourRatingStatsDto(total, avg, N(5), N(4), N(3), N(2), N(1));
    }

    public async Task<PagedResult<TourRatingByTourDto>> ListByTourAsync(int page, int size)
    {
        // Chỉ đánh giá CÓ gắn chuyến. Gom (chuyến, số sao) bằng MỘT câu GROUP BY ở SQL —
        // trả về tập nhóm nhỏ (≤ số chuyến × 5 bậc sao), KHÔNG nạp cả bảng đánh giá.
        var grouped = await repo.CountByAsync(
            r => new { TourId = r.TourDepartureId!.Value, r.Stars },
            r => r.TourDepartureId != null);

        // Cộng dồn theo chuyến trong bộ nhớ (tập rất nhỏ): tổng lượt + tổng sao → sao trung bình.
        var byTour = new Dictionary<Guid, (int Count, long StarSum)>();
        foreach (var (key, count) in grouped)
        {
            var cur = byTour.GetValueOrDefault(key.TourId);
            byTour[key.TourId] = (cur.Count + count, cur.StarSum + (long)key.Stars * count);
        }

        var ordered = byTour
            .Select(kv => new TourRatingByTourDto(
                kv.Key, kv.Value.Count, Math.Round((double)kv.Value.StarSum / kv.Value.Count, 1)))
            .OrderByDescending(x => x.RatingCount)
            .ThenBy(x => x.TourDepartureId)
            .ToList();

        var pageItems = ordered.Skip((page - 1) * size).Take(size).ToList();
        return new PagedResult<TourRatingByTourDto>(pageItems, ordered.Count, page, size);
    }

    public async Task<TourRatingDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<TourRatingDto> CreateAsync(CreateTourRatingDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new TourRating
        {
            TourDepartureId = dto.TourDepartureId,
            OrderId = dto.OrderId,
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            Stars = dto.Stars,
            Comment = dto.Comment,
            Status = dto.Status,
            SalesUserId = dto.SalesUserId,
            OperatorUserId = dto.OperatorUserId,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateTourRatingDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.CustomerName = dto.CustomerName;
        entity.CustomerPhone = dto.CustomerPhone;
        entity.Stars = dto.Stars;
        entity.Comment = dto.Comment;
        entity.Status = dto.Status;
        entity.SalesUserId = dto.SalesUserId;
        entity.OperatorUserId = dto.OperatorUserId;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static TourRatingDto Map(TourRating r) => new(
        r.Id, r.TourDepartureId, r.OrderId, r.CustomerName, r.CustomerPhone, r.Stars, r.Comment, r.Status,
        r.SalesUserId, r.OperatorUserId);
}
