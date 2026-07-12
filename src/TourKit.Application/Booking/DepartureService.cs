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
    public async Task<PagedResult<DepartureDto>> ListAsync(int page, int size, DepartureListFilter? filter = null)
    {
        var f = filter ?? new DepartureListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();
        var tt = string.IsNullOrWhiteSpace(f.TourType) ? null : f.TourType.Trim();

        // Lọc cột thật ở DB (loại tour, trạng thái, NV điều hành, đã đóng, ngày khởi hành); q lọc sau.
        var all = await departureRepo.ListAsync(d =>
            (tt == null || d.TourType == tt) &&
            (f.Status == null || d.Status == f.Status) &&
            (f.AssignedToUserId == null || d.AssignedToUserId == f.AssignedToUserId) &&
            (f.IsClosed == null || d.IsClosed == f.IsClosed) &&
            (f.DepartureFrom == null || d.DepartureDate >= f.DepartureFrom) &&
            (f.DepartureTo == null || d.DepartureDate <= f.DepartureTo));

        bool MatchQ(TourDeparture d) =>
            kw == null ||
            d.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            d.Title.Contains(kw, StringComparison.OrdinalIgnoreCase);

        var filtered = all.Where(MatchQ).OrderByDescending(d => d.DepartureDate).ToList();
        var pageItems = filtered.Skip((page - 1) * size).Take(size).Select(Map).ToList();
        return new PagedResult<DepartureDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<DepartureStatsDto> GetStatsAsync()
    {
        var all = await departureRepo.ListAsync();
        var now = DateTimeOffset.UtcNow;
        return new DepartureStatsDto(
            all.Count,
            all.Count(d => !d.IsClosed && d.DepartureDate != null && d.DepartureDate >= now),
            all.Count(d => d.IsClosed),
            all.Sum(d => d.TotalSlots));
    }

    public async Task<DepartureFilterOptionsDto> GetFilterOptionsAsync()
    {
        var tourTypes = (await departureRepo.ListAsync())
            .Where(d => !string.IsNullOrWhiteSpace(d.TourType))
            .Select(d => d.TourType!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.CurrentCulture)
            .ToList();
        return new DepartureFilterOptionsDto(tourTypes);
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

    /// <summary>
    /// Mở HÀNG LOẠT chuyến từ 1 mẫu (legacy BatchCreateTour): mỗi ngày trong Items → 1 chuyến,
    /// Code = "CodePrefix-STT". Tái dùng <see cref="CreateAsync"/> (kế thừa mẫu, tạo lịch trình). Idempotent-free:
    /// nếu 1 chuyến lỗi (trùng Code…) sẽ ném — client sửa rồi thử lại; các chuyến trước đó đã lưu.
    /// </summary>
    public async Task<BatchCreateResultDto> BatchCreateAsync(BatchCreateDeparturesDto dto)
    {
        if (dto.Items.Length == 0)
        {
            throw new ValidationAppException("Cần ít nhất 1 ngày khởi hành.");
        }

        if (string.IsNullOrWhiteSpace(dto.CodePrefix))
        {
            throw new ValidationAppException("Cần tiền tố mã chuyến (CodePrefix).");
        }

        var template = await templateRepo.GetByIdAsync(dto.TemplateId)
            ?? throw new ValidationAppException("Mẫu tour không tồn tại.");

        var title = string.IsNullOrWhiteSpace(dto.Title) ? template.Title : dto.Title.Trim();
        var created = new List<DepartureDto>(dto.Items.Length);
        for (var i = 0; i < dto.Items.Length; i++)
        {
            var item = dto.Items[i];
            created.Add(await CreateAsync(new CreateDepartureDto(
                dto.TemplateId, $"{dto.CodePrefix.Trim()}-{i + 1}", title,
                item.DepartureDate, item.EndDate, dto.TotalSlots)));
        }

        return new BatchCreateResultDto(created.Count, created.ToArray());
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
        d.Id, d.Code, d.Title, d.ParentTourId, d.DepartureDate, d.EndDate, d.TotalSlots, d.Status,
        d.TourType, d.AssignedToUserId, d.IsClosed);
}
