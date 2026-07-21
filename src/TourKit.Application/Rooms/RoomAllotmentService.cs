using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Rooms.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Rooms;

/// <summary>
/// Quỹ phòng / allotment (legacy "QUỸ PHÒNG KHÁCH SẠN") — tra cứu tồn (Quota/Booked) + giá NET theo ngày.
/// Grid client gom hàng theo NCC+dịch vụ, cột theo ngày; ô tô màu theo DayType. Enrich tên NCC từ string ref
/// (best-effort: khớp GUID → tên; không khớp → giữ nguyên chuỗi id legacy).
/// </summary>
public sealed class RoomAllotmentService(
    IRepository<RoomAllotment> repo,
    IRepository<Provider> providerRepo,
    IRoomAllotmentQueries queries,
    IValidator<CreateRoomAllotmentDto> createValidator,
    IValidator<UpdateRoomAllotmentDto> updateValidator) : IRoomAllotmentService
{
    /// <summary>
    /// Lọc/sắp/cắt trang đẩy hết xuống SQL qua <see cref="IRoomAllotmentQueries"/>. Bảng này ~36.000
    /// dòng và màn lưới lịch gọi hàm này NHIỀU LƯỢT để quét theo lô — nạp cả bảng mỗi lượt là sập.
    /// </summary>
    public async Task<PagedResult<RoomAllotmentDto>> ListAsync(int page, int size, RoomAllotmentListFilter? filter = null)
    {
        var (items, total) = await queries.PageAsync(filter ?? new RoomAllotmentListFilter(), page, size);
        var dtos = await MapManyAsync(items);
        return new PagedResult<RoomAllotmentDto>(dtos, total, page, size);
    }

    /// <summary>Gộp bằng COUNT/SUM ở SQL — không kéo dòng nào về bộ nhớ.</summary>
    public Task<RoomAllotmentStatsDto> GetStatsAsync(RoomAllotmentListFilter? filter = null)
        => queries.StatsAsync(filter ?? new RoomAllotmentListFilter());

    public async Task<RoomAllotmentDto> GetAsync(Guid id)
    {
        var a = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        return (await MapManyAsync([a]))[0];
    }

    public async Task<RoomAllotmentDto> CreateAsync(CreateRoomAllotmentDto dto)
    {
        var result = await createValidator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }

        var entity = new RoomAllotment
        {
            ProviderRef = dto.ProviderRef.Trim(),
            ServiceName = dto.ServiceName.Trim(),
            ProjectName = dto.ProjectName,
            Province = dto.Province,
            Market = dto.Market,
            Date = dto.Date,
            DayType = dto.DayType,
            Quota = dto.Quota,
            Booked = dto.Booked,
            Price = dto.Price,
            Rating = dto.Rating,
            Note = dto.Note,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();
        return (await MapManyAsync([entity]))[0];
    }

    public async Task UpdateAsync(Guid id, UpdateRoomAllotmentDto dto)
    {
        var result = await updateValidator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        entity.ProviderRef = dto.ProviderRef.Trim();
        entity.ServiceName = dto.ServiceName.Trim();
        entity.ProjectName = dto.ProjectName;
        entity.Province = dto.Province;
        entity.Market = dto.Market;
        entity.Date = dto.Date;
        entity.DayType = dto.DayType;
        entity.Quota = dto.Quota;
        entity.Booked = dto.Booked;
        entity.Price = dto.Price;
        entity.Rating = dto.Rating;
        entity.Note = dto.Note;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private async Task<List<RoomAllotmentDto>> MapManyAsync(IReadOnlyList<RoomAllotment> items)
    {
        // Chỉ tra tên cho những NCC XUẤT HIỆN trong tập truyền vào (một trang), thay vì nạp cả bảng NCC.
        var refIds = items
            .Select(a => a.ProviderRef)
            .Where(r => Guid.TryParse(r, out _))
            .Select(Guid.Parse)
            .ToHashSet();
        var providerNames = refIds.Count == 0
            ? []
            : (await providerRepo.ListAsync(p => refIds.Contains(p.Id))).ToDictionary(p => p.Id.ToString(), p => p.Name);

        return items.Select(a => new RoomAllotmentDto(
            a.Id, a.ProviderRef, ResolveRef(a.ProviderRef, providerNames), a.ServiceName,
            a.ProjectName, a.Province, a.Market,
            a.Date, a.DayType, a.Quota, a.Booked, Math.Max(0, a.Quota - a.Booked),
            a.Price, a.Rating, a.Note)).ToList();
    }

    /// <summary>Ref string: khớp GUID trong dict → tên; không khớp (id legacy) → giữ nguyên chuỗi.</summary>
    private static string? ResolveRef(string? refValue, IReadOnlyDictionary<string, string> names)
        => string.IsNullOrWhiteSpace(refValue) ? null : names.GetValueOrDefault(refValue, refValue);
}
