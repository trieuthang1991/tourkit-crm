using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Booking;

/// <summary>Đặt dịch vụ lẻ (hotel/vé/visa...) — CRUD phân trang, lọc theo loại + đơn. TotalAmount = SL × đơn giá.</summary>
public sealed class ServiceBookingService(
    IRepository<ServiceBooking> repo,
    IValidator<CreateServiceBookingDto> createValidator,
    IValidator<UpdateServiceBookingDto> updateValidator) : IServiceBookingService
{
    public async Task<PagedResult<ServiceBookingDto>> ListAsync(int page, int size, ServiceBookingListFilter? filter = null)
    {
        var f = filter ?? new ServiceBookingListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        // Lọc cột thật ở DB; q (mã/mô tả) lọc sau.
        var all = await repo.ListAsync(b =>
            (f.Type == null || b.Type == f.Type) &&
            (f.ProviderId == null || b.ProviderId == f.ProviderId) &&
            (f.OrderId == null || b.OrderId == f.OrderId) &&
            (f.Status == null || b.Status == f.Status) &&
            (f.DateFrom == null || b.StartDate >= f.DateFrom) &&
            (f.DateTo == null || b.StartDate <= f.DateTo));

        bool MatchQ(ServiceBooking b) =>
            kw == null ||
            b.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            b.Description.Contains(kw, StringComparison.OrdinalIgnoreCase);

        var filtered = all.Where(MatchQ).OrderByDescending(b => b.CreatedAt).ToList();
        var pageItems = filtered.Skip((page - 1) * size).Take(size).Select(Map).ToList();
        return new PagedResult<ServiceBookingDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<ServiceBookingStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        return new ServiceBookingStatsDto(
            all.Count,
            all.Count(b => b.Type == ServiceBookingType.Hotel),
            all.Count(b => b.Type == ServiceBookingType.Flight),
            all.Count(b => b.Type == ServiceBookingType.Visa),
            all.Count(b => b.Type == ServiceBookingType.Ticket),
            all.Count(b => b.Type == ServiceBookingType.Transfer),
            all.Count(b => b.Type == ServiceBookingType.Other),
            all.Sum(b => b.TotalAmount));
    }

    public async Task<ServiceBookingDto> CreateAsync(CreateServiceBookingDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new ServiceBooking
        {
            Code = dto.Code.Trim(),
            Type = dto.Type,
            OrderId = dto.OrderId,
            ProviderId = dto.ProviderId,
            Description = dto.Description.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            TotalAmount = dto.Quantity * dto.UnitPrice,
            Status = dto.Status,
            Note = dto.Note,
            RoomClassId = dto.RoomClassId,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateServiceBookingDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        entity.Code = dto.Code.Trim();
        entity.Type = dto.Type;
        entity.OrderId = dto.OrderId;
        entity.ProviderId = dto.ProviderId;
        entity.Description = dto.Description.Trim();
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.Quantity = dto.Quantity;
        entity.UnitPrice = dto.UnitPrice;
        entity.TotalAmount = dto.Quantity * dto.UnitPrice;
        entity.Status = dto.Status;
        entity.Note = dto.Note;
        entity.RoomClassId = dto.RoomClassId;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
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

    private static ServiceBookingDto Map(ServiceBooking b) =>
        new(b.Id, b.Code, b.Type, b.OrderId, b.ProviderId, b.Description,
            b.StartDate, b.EndDate, b.Quantity, b.UnitPrice, b.TotalAmount, b.Status, b.Note, b.RoomClassId);
}
