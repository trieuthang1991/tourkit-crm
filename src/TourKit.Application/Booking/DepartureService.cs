using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>Mở/liệt kê/xem/đóng chuyến khởi hành (TourDeparture) — legacy TPT (Kind = Departure).</summary>
public sealed class DepartureService(
    IRepository<TourDeparture> departureRepo,
    IRepository<TourTemplate> templateRepo,
    IRepository<TourItinerary> itineraryRepo,
    IValidator<CreateDepartureDto> createValidator) : IDepartureService
{
    public async Task<PagedResult<DepartureDto>> ListAsync(int page, int size)
    {
        var (items, total) = await departureRepo.PageAsync(page, size);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<DepartureDto>(dtos, total, page, size);
    }

    public async Task<DepartureDto> GetAsync(Guid id)
    {
        var departure = await departureRepo.GetByIdAsync(id);
        if (departure is null)
        {
            throw new NotFoundException();
        }

        return Map(departure);
    }

    /// <summary>Mở chuyến từ mẫu tour (nếu có) — kế thừa loại tour/sức chứa/lịch trình từ template.</summary>
    public async Task<DepartureDto> CreateAsync(CreateDepartureDto dto)
    {
        await Validate(createValidator, dto);

        var departure = new TourDeparture
        {
            Code = dto.Code.Trim(),
            Title = dto.Title.Trim(),
            ParentTourId = dto.TemplateId,
            DepartureDate = dto.DepartureDate,
            EndDate = dto.EndDate,
            TotalSlots = dto.TotalSlots,
        };

        if (dto.TemplateId is { } templateId)
        {
            var template = await templateRepo.GetByIdAsync(templateId);
            if (template is not null)
            {
                departure.TourType = template.TourType;
                if (departure.TotalSlots == 0)
                {
                    departure.TotalSlots = template.TotalSlots;
                }
            }
        }

        await departureRepo.AddAsync(departure);

        if (dto.TemplateId is { } tplId)
        {
            var days = await itineraryRepo.ListAsync(i => i.TourId == tplId);
            foreach (var day in days.OrderBy(d => d.DayIndex))
            {
                await itineraryRepo.AddAsync(new TourItinerary
                {
                    TourId = departure.Id, DayIndex = day.DayIndex, Title = day.Title, Detail = day.Detail,
                });
            }
        }

        await departureRepo.SaveChangesAsync();
        await itineraryRepo.SaveChangesAsync();

        return Map(departure);
    }

    /// <summary>Đóng chuyến (chốt sổ) — legacy StatusCloseTour. Đóng rồi không đặt thêm chỗ được.</summary>
    public async Task<DepartureDto> CloseAsync(Guid id)
    {
        var departure = await departureRepo.GetByIdAsync(id);
        if (departure is null)
        {
            throw new NotFoundException();
        }

        if (departure.IsClosed)
        {
            throw new ConflictException("Chuyến đã đóng.");
        }

        departure.IsClosed = true;
        departure.ClosedAt = DateTimeOffset.UtcNow;
        departureRepo.Update(departure);
        await departureRepo.SaveChangesAsync();

        return Map(departure);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static DepartureDto Map(TourDeparture d) => new(
        d.Id, d.Code, d.Title, d.ParentTourId, d.DepartureDate, d.EndDate, d.TotalSlots, d.Status);
}
