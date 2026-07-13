using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Flights.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Flights;

/// <summary>
/// Vé máy bay đoàn (legacy "Quản lý Vé Đoàn"): quỹ vé theo PNR + hành trình + gán tour + theo dõi
/// vé dùng/còn, chi/thanh toán/bảo lưu. Enrich tên Thị trường/NCC/Đơn từ string ref (best-effort:
/// khớp GUID → tên; không khớp → giữ nguyên chuỗi id legacy).
/// </summary>
public sealed class FlightTicketService(
    IRepository<FlightTicket> repo,
    IRepository<MarketType> marketRepo,
    IRepository<Provider> providerRepo,
    IRepository<Order> orderRepo,
    IRepository<TourDeparture> departureRepo,
    IValidator<CreateFlightTicketDto> createValidator) : IFlightTicketService
{
    public async Task<PagedResult<FlightTicketDto>> ListAsync(int page, int size, FlightTicketListFilter? filter = null)
    {
        var filtered = await QueryAsync(filter);
        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();
        var dtos = await MapManyAsync(pageItems);
        return new PagedResult<FlightTicketDto>(dtos, filtered.Count, page, size);
    }

    public async Task<FlightTicketStatsDto> GetStatsAsync(FlightTicketListFilter? filter = null)
    {
        var all = await QueryAsync(filter);
        return new FlightTicketStatsDto(
            all.Count,
            all.Count(t => !string.IsNullOrWhiteSpace(t.OrderRef)),
            all.Count(t => string.IsNullOrWhiteSpace(t.OrderRef)),
            all.Sum(t => t.Quantity),
            all.Sum(t => t.UsedQuantity),
            all.Sum(t => t.Quantity - t.UsedQuantity),
            all.Sum(t => t.TotalCost),
            all.Sum(t => t.PaidAmount),
            all.Sum(t => t.TotalCost - t.PaidAmount),
            all.Sum(t => t.ReservedAmount));
    }

    public async Task<FlightTicketDto> GetAsync(Guid id)
    {
        var t = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        return (await MapManyAsync([t]))[0];
    }

    public async Task<FlightTicketDto> CreateAsync(CreateFlightTicketDto dto)
    {
        var result = await createValidator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }

        var entity = new FlightTicket
        {
            Pnr = dto.Pnr.Trim(),
            MarketRef = dto.MarketRef,
            ProviderRef = dto.ProviderRef,
            TourType = dto.TourType,
            Days = dto.Days,
            DepartureDate = dto.DepartureDate,
            Quantity = dto.Quantity,
            TotalCost = dto.TotalCost,
            ReservedAmount = dto.ReservedAmount,
            Note = dto.Note,
            ItineraryJson = new FlightItinerary { Segments = dto.Segments ?? [] }.ToJsonOrNull(),
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();
        return (await MapManyAsync([entity]))[0];
    }

    public async Task UpdateAsync(Guid id, UpdateFlightTicketDto dto)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        if (dto.UsedQuantity < 0 || dto.UsedQuantity > dto.Quantity)
        {
            throw new ValidationAppException("Số vé đã dùng phải trong khoảng 0..số lượng.");
        }

        entity.Pnr = dto.Pnr.Trim();
        entity.MarketRef = dto.MarketRef;
        entity.ProviderRef = dto.ProviderRef;
        entity.TourType = dto.TourType;
        entity.Days = dto.Days;
        entity.DepartureDate = dto.DepartureDate;
        entity.Quantity = dto.Quantity;
        entity.UsedQuantity = dto.UsedQuantity;
        entity.OrderRef = dto.OrderRef;
        entity.TotalCost = dto.TotalCost;
        entity.PaidAmount = dto.PaidAmount;
        entity.ReservedAmount = dto.ReservedAmount;
        entity.Status = dto.Status;
        entity.Note = dto.Note;
        entity.ItineraryJson = new FlightItinerary { Segments = dto.Segments ?? [] }.ToJsonOrNull();
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task<FlightTicketDto> AssignAsync(Guid id, AssignFlightTicketDto dto)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        entity.OrderRef = string.IsNullOrWhiteSpace(dto.OrderRef) ? null : dto.OrderRef.Trim();
        repo.Update(entity);
        await repo.SaveChangesAsync();
        return (await MapManyAsync([entity]))[0];
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private async Task<List<FlightTicket>> QueryAsync(FlightTicketListFilter? filter)
    {
        var f = filter ?? new FlightTicketListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(t =>
            (f.MarketRef == null || t.MarketRef == f.MarketRef) &&
            (f.ProviderRef == null || t.ProviderRef == f.ProviderRef) &&
            (f.TourType == null || t.TourType == f.TourType) &&
            (f.Days == null || t.Days == f.Days) &&
            (f.DepartureFrom == null || (t.DepartureDate != null && t.DepartureDate >= f.DepartureFrom)));

        return all
            .Where(t => kw == null || t.Pnr.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .Where(t => f.Assigned == null || (f.Assigned.Value ? !string.IsNullOrWhiteSpace(t.OrderRef) : string.IsNullOrWhiteSpace(t.OrderRef)))
            .OrderByDescending(t => t.DepartureDate ?? DateTimeOffset.MinValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToList();
    }

    private async Task<List<FlightTicketDto>> MapManyAsync(IReadOnlyList<FlightTicket> items)
    {
        var marketNames = (await marketRepo.ListAsync()).ToDictionary(m => m.Id.ToString(), m => m.Name);
        var providerNames = (await providerRepo.ListAsync()).ToDictionary(p => p.Id.ToString(), p => p.Name);

        var orderRefs = items.Where(t => !string.IsNullOrWhiteSpace(t.OrderRef)).Select(t => t.OrderRef!).ToHashSet();
        var orders = (await orderRepo.ListAsync(o => orderRefs.Contains(o.Id.ToString())))
            .ToDictionary(o => o.Id.ToString(), o => o);
        var departureTitles = (await departureRepo.ListAsync()).ToDictionary(d => d.Id, d => d.Title);

        return items.Select(t =>
        {
            string? orderCode = null, orderName = null;
            if (!string.IsNullOrWhiteSpace(t.OrderRef) && orders.TryGetValue(t.OrderRef, out var o))
            {
                orderCode = o.Code;
                orderName = departureTitles.GetValueOrDefault(o.TourDepartureId);
            }
            else if (!string.IsNullOrWhiteSpace(t.OrderRef))
            {
                orderCode = t.OrderRef; // id legacy chưa migrate → giữ nguyên
            }

            var segments = FlightItinerary.Parse(t.ItineraryJson).Segments;
            return new FlightTicketDto(
                t.Id, t.Pnr,
                t.MarketRef, ResolveRef(t.MarketRef, marketNames),
                t.ProviderRef, ResolveRef(t.ProviderRef, providerNames),
                t.TourType, t.Days, t.DepartureDate,
                t.Quantity, t.UsedQuantity, t.Quantity - t.UsedQuantity,
                t.OrderRef, orderCode, orderName,
                t.TotalCost, t.PaidAmount, t.TotalCost - t.PaidAmount, t.ReservedAmount,
                t.Status, t.Note, segments);
        }).ToList();
    }

    /// <summary>Ref string: khớp GUID trong dict → tên; không khớp (id legacy) → giữ nguyên chuỗi.</summary>
    private static string? ResolveRef(string? refValue, IReadOnlyDictionary<string, string> names)
        => string.IsNullOrWhiteSpace(refValue) ? null : names.GetValueOrDefault(refValue, refValue);
}
