using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Phân công HDV cho chuyến (legacy TourGuide) — CRUD phân trang, lọc theo chuyến.
/// Cô lập tenant do AppDbContext lo (global filter + interceptor). Validate chuyến + HDV tồn tại khi ghi.
/// </summary>
public sealed class GuideAssignmentService(
    IRepository<TourGuideAssignment> repo,
    IRepository<TourDeparture> departureRepo,
    IRepository<Provider> providerRepo,
    IValidator<CreateGuideAssignmentDto> createValidator,
    IValidator<UpdateGuideAssignmentDto> updateValidator) : IGuideAssignmentService
{
    public async Task<PagedResult<GuideAssignmentDto>> ListAsync(int page, int size, GuideAssignmentListFilter? filter = null)
    {
        var f = filter ?? new GuideAssignmentListFilter();
        var all = await repo.ListAsync(a =>
            (f.ProviderId == null || a.ProviderId == f.ProviderId) &&
            (f.DepartureId == null || a.TourDepartureId == f.DepartureId) &&
            (f.Status == null || a.Status == f.Status) &&
            (f.DateFrom == null || a.TimeGo >= f.DateFrom) &&
            (f.DateTo == null || a.TimeGo <= f.DateTo));

        var ordered = all.OrderByDescending(a => a.TimeGo ?? a.CreatedAt).ToList();
        var pageItems = ordered.Skip((page - 1) * size).Take(size).ToList();

        // Làm giàu tên HDV + tên/mã chuyến theo lô.
        var providerIds = pageItems.Select(a => a.ProviderId).ToHashSet();
        var departureIds = pageItems.Select(a => a.TourDepartureId).ToHashSet();
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id)))
            .ToDictionary(p => p.Id, p => p.Name);
        var departures = (await departureRepo.ListAsync(d => departureIds.Contains(d.Id)))
            .ToDictionary(d => d.Id, d => d);

        var dtos = pageItems.Select(a =>
        {
            departures.TryGetValue(a.TourDepartureId, out var dep);
            return Map(a) with
            {
                ProviderName = providerNames.GetValueOrDefault(a.ProviderId),
                DepartureTitle = dep?.Title,
                DepartureCode = dep?.Code,
            };
        }).ToList();
        return new PagedResult<GuideAssignmentDto>(dtos, ordered.Count, page, size);
    }

    public async Task<GuideAssignmentStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        return new GuideAssignmentStatsDto(
            all.Count,
            all.Count(a => a.Status == 1),
            all.Count(a => a.Status == 2),
            all.Select(a => a.ProviderId).Distinct().Count());
    }

    public async Task<GuideAssignmentDto> CreateAsync(CreateGuideAssignmentDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureDepartureExists(dto.TourDepartureId);
        await EnsureProviderExists(dto.ProviderId);

        var assignment = new TourGuideAssignment
        {
            TourDepartureId = dto.TourDepartureId,
            ProviderId = dto.ProviderId,
            TimeGo = dto.TimeGo,
            TimeCome = dto.TimeCome,
            TimeReturn = dto.TimeReturn,
            Note = dto.Note,
            Status = dto.Status,
        };
        await repo.AddAsync(assignment);
        await repo.SaveChangesAsync();

        return Map(assignment);
    }

    public async Task UpdateAsync(Guid id, UpdateGuideAssignmentDto dto)
    {
        await Validate(updateValidator, dto);
        await EnsureProviderExists(dto.ProviderId);

        var assignment = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        assignment.ProviderId = dto.ProviderId;
        assignment.TimeGo = dto.TimeGo;
        assignment.TimeCome = dto.TimeCome;
        assignment.TimeReturn = dto.TimeReturn;
        assignment.Note = dto.Note;
        assignment.Status = dto.Status;
        repo.Update(assignment);
        await repo.SaveChangesAsync();
    }

    public async Task<GuideAssignmentDto> HandoverAsync(Guid id, HandoverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            throw new ValidationAppException("Cần nội dung bàn giao.");
        }

        var assignment = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        assignment.HandoverContent = dto.Content.Trim();
        assignment.HandedOverAt = DateTimeOffset.UtcNow;
        repo.Update(assignment);
        await repo.SaveChangesAsync();

        return Map(assignment);
    }

    public async Task DeleteAsync(Guid id)
    {
        var assignment = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(assignment);
        await repo.SaveChangesAsync();
    }

    private async Task EnsureDepartureExists(Guid departureId)
    {
        if (!await departureRepo.AnyAsync(d => d.Id == departureId))
        {
            throw new ValidationAppException("Chuyến (departure) không tồn tại.");
        }
    }

    private async Task EnsureProviderExists(Guid providerId)
    {
        if (!await providerRepo.AnyAsync(p => p.Id == providerId))
        {
            throw new ValidationAppException("HDV (provider) không tồn tại.");
        }
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static GuideAssignmentDto Map(TourGuideAssignment a) =>
        new(a.Id, a.TourDepartureId, a.ProviderId, a.TimeGo, a.TimeCome, a.TimeReturn, a.Note, a.Status,
            a.HandoverContent, a.HandedOverAt);
}
