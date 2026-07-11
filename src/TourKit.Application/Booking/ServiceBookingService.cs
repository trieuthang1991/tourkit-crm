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
    public async Task<PagedResult<ServiceBookingDto>> ListAsync(int page, int size, ServiceBookingType? type, Guid? orderId)
    {
        var (items, total) = (type, orderId) switch
        {
            ({ } t, { } oid) => await repo.PageAsync(page, size, b => b.Type == t && b.OrderId == oid),
            ({ } t, null) => await repo.PageAsync(page, size, b => b.Type == t),
            (null, { } oid) => await repo.PageAsync(page, size, b => b.OrderId == oid),
            _ => await repo.PageAsync(page, size),
        };
        return new PagedResult<ServiceBookingDto>(items.Select(Map).ToList(), total, page, size);
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
            b.StartDate, b.EndDate, b.Quantity, b.UnitPrice, b.TotalAmount, b.Status, b.Note);
}
