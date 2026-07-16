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
    IValidator<CreateRoomAllotmentDto> createValidator,
    IValidator<UpdateRoomAllotmentDto> updateValidator) : IRoomAllotmentService
{
    public async Task<PagedResult<RoomAllotmentDto>> ListAsync(int page, int size, RoomAllotmentListFilter? filter = null)
    {
        var filtered = await QueryAsync(filter);
        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();
        var dtos = await MapManyAsync(pageItems);
        return new PagedResult<RoomAllotmentDto>(dtos, filtered.Count, page, size);
    }

    public async Task<RoomAllotmentStatsDto> GetStatsAsync(RoomAllotmentListFilter? filter = null)
    {
        var all = await QueryAsync(filter);
        return new RoomAllotmentStatsDto(
            all.Count,
            all.Select(a => a.ProviderRef).Distinct().Count(),
            all.Sum(a => a.Quota),
            all.Sum(a => a.Booked),
            all.Sum(a => Math.Max(0, a.Quota - a.Booked)),
            all.Count(a => a.DayType == 0),
            all.Count(a => a.DayType == 1),
            all.Count(a => a.DayType == 2),
            all.Count(a => a.DayType == 3));
    }

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

    private async Task<List<RoomAllotment>> QueryAsync(RoomAllotmentListFilter? filter)
    {
        var f = filter ?? new RoomAllotmentListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(a =>
            (f.ProviderRef == null || a.ProviderRef == f.ProviderRef) &&
            (f.Province == null || a.Province == f.Province) &&
            (f.Market == null || a.Market == f.Market) &&
            (f.Rating == null || a.Rating == f.Rating) &&
            (f.DateFrom == null || a.Date >= f.DateFrom) &&
            (f.DateTo == null || a.Date <= f.DateTo));

        return all
            .Where(a => kw == null
                || a.ServiceName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (a.ProjectName != null && a.ProjectName.Contains(kw, StringComparison.OrdinalIgnoreCase))
                || (a.Province != null && a.Province.Contains(kw, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(a => a.ProviderRef)
            .ThenBy(a => a.ServiceName)
            .ThenBy(a => a.Date)
            .ToList();
    }

    private async Task<List<RoomAllotmentDto>> MapManyAsync(IReadOnlyList<RoomAllotment> items)
    {
        var providerNames = (await providerRepo.ListAsync()).ToDictionary(p => p.Id.ToString(), p => p.Name);

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
