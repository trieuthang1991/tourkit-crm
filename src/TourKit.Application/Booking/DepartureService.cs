using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Booking;

/// <summary>Mở/liệt kê/xem/đóng chuyến khởi hành (TourDeparture) — legacy TPT (Kind = Departure).</summary>
public sealed class DepartureService(
    IRepository<TourDeparture> departureRepo,
    IRepository<TourTemplate> templateRepo,
    IRepository<TourItinerary> itineraryRepo,
    IRepository<Order> orderRepo,
    IRepository<TourCustomer> seatRepo,
    IValidator<CreateDepartureDto> createValidator,
    IValidator<UpdateDepartureDto> updateValidator) : IDepartureService
{
    public async Task<PagedResult<DepartureDto>> ListAsync(int page, int size, DepartureListFilter? filter = null)
    {
        var f = filter ?? new DepartureListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();
        var tt = string.IsNullOrWhiteSpace(f.TourType) ? null : f.TourType.Trim();

        // Lọc cột thật ở DB (loại tour, trạng thái, NV điều hành, đã đóng, ngày khởi hành); q lọc sau.
        var all = await departureRepo.ListAsync(d =>
            (tt == null || d.TourType == tt) &&
            (f.Category == null || d.Category == f.Category) &&
            (f.Status == null || d.Status == f.Status) &&
            (f.AssignedToUserId == null || d.AssignedToUserId == f.AssignedToUserId) &&
            (f.IsClosed == null || d.IsClosed == f.IsClosed) &&
            (f.DepartureFrom == null || d.DepartureDate >= f.DepartureFrom) &&
            (f.DepartureTo == null || d.DepartureDate <= f.DepartureTo) &&
            (f.EndFrom == null || d.EndDate >= f.EndFrom) &&
            (f.EndTo == null || d.EndDate <= f.EndTo));

        bool MatchQ(TourDeparture d) =>
            kw == null ||
            d.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            d.Title.Contains(kw, StringComparison.OrdinalIgnoreCase);

        // Sắp xếp bám staging (chỉ theo cột có ở model chuyến): ngày khởi hành / số chỗ / mã.
        var matched = all.Where(MatchQ);
        var filtered = (f.Sort switch
        {
            "dateAsc" => matched.OrderBy(d => d.DepartureDate),
            "slots" => matched.OrderByDescending(d => d.TotalSlots),
            "code" => matched.OrderBy(d => d.Code),
            _ => matched.OrderByDescending(d => d.DepartureDate),   // dateDesc mặc định
        }).ToList();
        var page1 = filtered.Skip((page - 1) * size).Take(size).ToList();

        // Enrich CHỈ trang hiện tại (bám cột staging): Giá (từ template) + tách chỗ Giữ/Bán/Còn (từ đơn của chuyến).
        var tplIds = page1.Where(d => d.ParentTourId is not null).Select(d => d.ParentTourId!.Value).Distinct().ToList();
        var prices = tplIds.Count == 0 ? new Dictionary<Guid, decimal>()
            : (await templateRepo.ListAsync(t => tplIds.Contains(t.Id))).ToDictionary(t => t.Id, t => t.PriceAdult);

        var depIds = page1.Select(d => d.Id).ToList();
        var pageOrders = await orderRepo.ListAsync(o => depIds.Contains(o.TourDepartureId));
        var orderIds = pageOrders.Select(o => o.Id).ToHashSet();
        var orderDep = pageOrders.ToDictionary(o => o.Id, o => o.TourDepartureId);
        var seats = orderIds.Count == 0 ? [] : await seatRepo.ListAsync(s => orderIds.Contains(s.OrderId));

        var held = new Dictionary<Guid, int>();
        var sold = new Dictionary<Guid, int>();
        foreach (var s in seats)
        {
            if (!orderDep.TryGetValue(s.OrderId, out var depId)) { continue; }
            var st = BookingMath.DeriveSeatStatus(s);
            if (st is SeatStatus.Held or SeatStatus.HeldConfirmed) { held[depId] = held.GetValueOrDefault(depId) + s.Quantity; }
            else if (st is SeatStatus.Deposited or SeatStatus.Paid) { sold[depId] = sold.GetValueOrDefault(depId) + s.Quantity; }
        }

        var pageItems = page1.Select(d =>
        {
            var h = held.GetValueOrDefault(d.Id);
            var so = sold.GetValueOrDefault(d.Id);
            var price = d.ParentTourId is { } tid && prices.TryGetValue(tid, out var p) ? p : 0m;
            return Map(d) with { Price = price, SeatHeld = h, SeatSold = so, SeatRemaining = Math.Max(0, d.TotalSlots - h - so), ClosedAt = d.ClosedAt, Category = d.Category };
        }).ToList();

        return new PagedResult<DepartureDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<DepartureStatsDto> GetStatsAsync()
    {
        // Đếm/cộng ở SQL. Các tiêu chí ở đây không cùng một bậc nên phải tách COUNT có điều kiện.
        var now = DateTimeOffset.UtcNow;
        var byClosed = await departureRepo.CountByAsync(d => d.IsClosed);

        return new DepartureStatsDto(
            byClosed.Values.Sum(),
            await departureRepo.CountAsync(d => !d.IsClosed && d.DepartureDate != null && d.DepartureDate >= now),
            byClosed.GetValueOrDefault(true),
            await departureRepo.SumIntAsync(d => d.TotalSlots));
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

    /// <summary>Chốt sổ hoa hồng chuyến — legacy `update tours set StatusComission=1`.
    /// Khoá hoa hồng: đã chốt thì báo cáo hoa hồng coi như đã quyết toán, không chốt lại.</summary>
    public async Task<DepartureDto> CloseCommissionAsync(Guid id)
    {
        var departure = await departureRepo.GetByIdAsync(id);
        if (departure is null)
        {
            throw new NotFoundException();
        }

        if (departure.CommissionClosed)
        {
            throw new ConflictException("Hoa hồng chuyến đã được chốt sổ.");
        }

        departure.CommissionClosed = true;
        departure.CommissionClosedAt = DateTimeOffset.UtcNow;
        departureRepo.Update(departure);
        await departureRepo.SaveChangesAsync();

        return Map(departure);
    }

    /// <summary>Mở lại sổ hoa hồng đã chốt (điều chỉnh/sửa sai) — legacy set StatusComission=0.</summary>
    public async Task<DepartureDto> ReopenCommissionAsync(Guid id)
    {
        var departure = await departureRepo.GetByIdAsync(id);
        if (departure is null)
        {
            throw new NotFoundException();
        }

        if (!departure.CommissionClosed)
        {
            throw new ConflictException("Hoa hồng chuyến chưa chốt sổ.");
        }

        departure.CommissionClosed = false;
        departure.CommissionClosedAt = null;
        departureRepo.Update(departure);
        await departureRepo.SaveChangesAsync();

        return Map(departure);
    }

    /// <summary>Sửa chuyến đang mở. Xem <see cref="IDepartureService.UpdateAsync"/> về luật chuyến đã đóng.</summary>
    public async Task<DepartureDto> UpdateAsync(Guid id, UpdateDepartureDto dto)
    {
        var departure = await departureRepo.GetByIdAsync(id) ?? throw new NotFoundException();

        if (departure.IsClosed)
        {
            throw new ConflictException("Chuyến đã đóng, không sửa được nữa.");
        }

        await Validate(updateValidator, dto);

        departure.Code = dto.Code.Trim();
        departure.Title = dto.Title.Trim();
        departure.DepartureDate = dto.DepartureDate;
        departure.EndDate = dto.EndDate;
        departure.TotalSlots = dto.TotalSlots;

        // KHÔNG đụng tới ParentTourId: mẫu tour đã chép lịch trình sang chuyến lúc tạo. Xem
        // UpdateDepartureDto về lý do không cho đổi mẫu khi sửa.
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
        d.TourType, d.AssignedToUserId, d.IsClosed,
        ClosedAt: d.ClosedAt, CommissionClosed: d.CommissionClosed, CommissionClosedAt: d.CommissionClosedAt);
}
