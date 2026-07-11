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
    public async Task<PagedResult<GuideAssignmentDto>> ListAsync(int page, int size, Guid? departureId)
    {
        var (items, total) = departureId is { } depId
            ? await repo.PageAsync(page, size, a => a.TourDepartureId == depId)
            : await repo.PageAsync(page, size);
        return new PagedResult<GuideAssignmentDto>(items.Select(Map).ToList(), total, page, size);
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
        new(a.Id, a.TourDepartureId, a.ProviderId, a.TimeGo, a.TimeCome, a.TimeReturn, a.Note, a.Status);
}
