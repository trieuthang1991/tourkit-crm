using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Flights.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Flights;

/// <summary>
/// Vé máy bay lẻ (legacy "Quản lý Vé máy bay lẻ") — lưới vận hành vé cá nhân với P/L riêng từng vé:
/// thu (SellAmount/ReceivedAmount) · chi (TotalCost/PaidAmount) · lợi nhuận · công nợ + hạn chi. Sub-tab
/// bám hệ cũ (chờ chi/đến hạn 24h/quá hạn/chưa chi hết/thành công/chưa thu hết). Enrich tên NCC/Đơn từ
/// string ref (best-effort: khớp GUID → tên; không khớp → giữ nguyên chuỗi id legacy).
/// </summary>
public sealed class FlightTicketIndividualService(
    IRepository<FlightTicketIndividual> repo,
    IRepository<Provider> providerRepo,
    IRepository<Order> orderRepo,
    IValidator<CreateFlightTicketIndividualDto> createValidator,
    IValidator<UpdateFlightTicketIndividualDto> updateValidator) : IFlightTicketIndividualService
{
    public async Task<PagedResult<FlightTicketIndividualDto>> ListAsync(int page, int size, FlightTicketIndividualListFilter? filter = null)
    {
        var filtered = await QueryAsync(filter);
        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();
        var dtos = await MapManyAsync(pageItems);
        return new PagedResult<FlightTicketIndividualDto>(dtos, filtered.Count, page, size);
    }

    public async Task<FlightTicketIndividualStatsDto> GetStatsAsync(FlightTicketIndividualListFilter? filter = null)
    {
        var all = await QueryAsync(filter);
        var now = DateTimeOffset.UtcNow;
        return new FlightTicketIndividualStatsDto(
            all.Count,
            all.Count(t => MatchTab(t, "new", now)),
            all.Count(t => MatchTab(t, "approved", now)),
            all.Count(t => MatchTab(t, "rejected", now)),
            all.Count(t => MatchTab(t, "pending-pay", now)),
            all.Count(t => MatchTab(t, "due-soon", now)),
            all.Count(t => MatchTab(t, "overdue", now)),
            all.Count(t => MatchTab(t, "partial-pay", now)),
            all.Count(t => MatchTab(t, "success", now)),
            all.Count(t => MatchTab(t, "partial-receive", now)),
            all.Sum(t => t.SellAmount),
            all.Sum(t => t.ReceivedAmount),
            all.Sum(t => t.SellAmount - t.ReceivedAmount),
            all.Sum(t => t.TotalCost),
            all.Sum(t => t.PaidAmount),
            all.Sum(t => t.TotalCost - t.PaidAmount),
            all.Sum(t => t.SellAmount - t.TotalCost));
    }

    public async Task<FlightTicketIndividualDto> GetAsync(Guid id)
    {
        var t = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        return (await MapManyAsync([t]))[0];
    }

    public async Task<FlightTicketIndividualDto> CreateAsync(CreateFlightTicketIndividualDto dto)
    {
        var result = await createValidator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }

        var entity = new FlightTicketIndividual
        {
            Code = dto.Code.Trim(),
            TicketCode = dto.TicketCode,
            Pnr = dto.Pnr.Trim(),
            CustomerName = dto.CustomerName.Trim(),
            OrderRef = dto.OrderRef,
            ProviderRef = dto.ProviderRef,
            TripType = dto.TripType,
            Route = dto.Route,
            DepartDate = dto.DepartDate,
            ReturnDate = dto.ReturnDate,
            SellAmount = dto.SellAmount,
            ReceivedAmount = dto.ReceivedAmount,
            TotalCost = dto.TotalCost,
            PaidAmount = dto.PaidAmount,
            PaymentDueDate = dto.PaymentDueDate,
            Status = dto.Status,
            AssigneeRef = dto.AssigneeRef,
            Note = dto.Note,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();
        return (await MapManyAsync([entity]))[0];
    }

    public async Task UpdateAsync(Guid id, UpdateFlightTicketIndividualDto dto)
    {
        var result = await updateValidator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        entity.Code = dto.Code.Trim();
        entity.TicketCode = dto.TicketCode;
        entity.Pnr = dto.Pnr.Trim();
        entity.CustomerName = dto.CustomerName.Trim();
        entity.OrderRef = dto.OrderRef;
        entity.ProviderRef = dto.ProviderRef;
        entity.TripType = dto.TripType;
        entity.Route = dto.Route;
        entity.DepartDate = dto.DepartDate;
        entity.ReturnDate = dto.ReturnDate;
        entity.SellAmount = dto.SellAmount;
        entity.ReceivedAmount = dto.ReceivedAmount;
        entity.TotalCost = dto.TotalCost;
        entity.PaidAmount = dto.PaidAmount;
        entity.PaymentDueDate = dto.PaymentDueDate;
        entity.Status = dto.Status;
        entity.AssigneeRef = dto.AssigneeRef;
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

    private async Task<List<FlightTicketIndividual>> QueryAsync(FlightTicketIndividualListFilter? filter)
    {
        var f = filter ?? new FlightTicketIndividualListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();
        var now = DateTimeOffset.UtcNow;

        var all = await repo.ListAsync(t =>
            (f.ProviderRef == null || t.ProviderRef == f.ProviderRef) &&
            (f.Status == null || t.Status == f.Status) &&
            (f.DepartFrom == null || (t.DepartDate != null && t.DepartDate >= f.DepartFrom)) &&
            (f.DepartTo == null || (t.DepartDate != null && t.DepartDate <= f.DepartTo)));

        return all
            .Where(t => kw == null
                || t.Code.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || t.Pnr.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || t.CustomerName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (t.TicketCode != null && t.TicketCode.Contains(kw, StringComparison.OrdinalIgnoreCase)))
            .Where(t => string.IsNullOrWhiteSpace(f.Tab) || MatchTab(t, f.Tab!, now))
            .OrderByDescending(t => t.DepartDate ?? DateTimeOffset.MinValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToList();
    }

    /// <summary>Khớp sub-tab hệ cũ. PayableRemaining = TotalCost − PaidAmount; ReceivableRemaining = SellAmount − ReceivedAmount.</summary>
    private static bool MatchTab(FlightTicketIndividual t, string tab, DateTimeOffset now) => tab switch
    {
        "new" => t.Status == 0,
        "approved" => t.Status == 1,
        "rejected" => t.Status == 2,
        "pending-pay" => t.TotalCost > 0 && t.PaidAmount <= 0,
        "partial-pay" => t.PaidAmount > 0 && t.PaidAmount < t.TotalCost,
        "success" => t.TotalCost > 0 && t.PaidAmount >= t.TotalCost && t.ReceivedAmount >= t.SellAmount,
        "partial-receive" => t.SellAmount > 0 && t.ReceivedAmount < t.SellAmount,
        "due-soon" => t.TotalCost - t.PaidAmount > 0 && t.PaymentDueDate != null
            && t.PaymentDueDate >= now && t.PaymentDueDate <= now.AddHours(24),
        "overdue" => t.TotalCost - t.PaidAmount > 0 && t.PaymentDueDate != null && t.PaymentDueDate < now,
        _ => true,
    };

    private async Task<List<FlightTicketIndividualDto>> MapManyAsync(IReadOnlyList<FlightTicketIndividual> items)
    {
        var providerNames = (await providerRepo.ListAsync()).ToDictionary(p => p.Id.ToString(), p => p.Name);

        var orderRefs = items.Where(t => !string.IsNullOrWhiteSpace(t.OrderRef)).Select(t => t.OrderRef!).ToHashSet();
        var orderCodes = (await orderRepo.ListAsync(o => orderRefs.Contains(o.Id.ToString())))
            .ToDictionary(o => o.Id.ToString(), o => o.Code);

        return items.Select(t =>
        {
            string? orderCode = null;
            if (!string.IsNullOrWhiteSpace(t.OrderRef))
            {
                orderCode = orderCodes.GetValueOrDefault(t.OrderRef, t.OrderRef); // id legacy chưa migrate → giữ nguyên
            }

            return new FlightTicketIndividualDto(
                t.Id, t.Code, t.TicketCode, t.Pnr, t.CustomerName,
                t.OrderRef, orderCode, t.ProviderRef, ResolveRef(t.ProviderRef, providerNames),
                t.TripType, t.Route, t.DepartDate, t.ReturnDate,
                t.SellAmount, t.ReceivedAmount, t.SellAmount - t.ReceivedAmount,
                t.TotalCost, t.PaidAmount, t.TotalCost - t.PaidAmount, t.SellAmount - t.TotalCost,
                t.PaymentDueDate, t.Status, t.AssigneeRef, t.Note);
        }).ToList();
    }

    /// <summary>Ref string: khớp GUID trong dict → tên; không khớp (id legacy) → giữ nguyên chuỗi.</summary>
    private static string? ResolveRef(string? refValue, IReadOnlyDictionary<string, string> names)
        => string.IsNullOrWhiteSpace(refValue) ? null : names.GetValueOrDefault(refValue, refValue);
}
