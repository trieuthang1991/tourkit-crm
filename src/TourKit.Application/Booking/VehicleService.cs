using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>Xe (Vehicle) — CRUD phân trang, không có endpoint Get(id) riêng ở hệ cũ.</summary>
public sealed class VehicleService(
    IRepository<Vehicle> repo,
    IValidator<CreateVehicleDto> createValidator,
    IValidator<UpdateVehicleDto> updateValidator) : IVehicleService
{
    public async Task<PagedResult<VehicleDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<VehicleDto>(dtos, total, page, size);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto)
    {
        await Validate(createValidator, dto);

        var vehicle = new Vehicle
        {
            Name = dto.Name.Trim(),
            FirmName = dto.FirmName,
            SeatType = dto.SeatType,
            Status = dto.Status,
        };
        await repo.AddAsync(vehicle);
        await repo.SaveChangesAsync();

        return Map(vehicle);
    }

    public async Task UpdateAsync(Guid id, UpdateVehicleDto dto)
    {
        await Validate(updateValidator, dto);

        var vehicle = await repo.GetByIdAsync(id);
        if (vehicle is null)
        {
            throw new NotFoundException();
        }

        vehicle.Name = dto.Name.Trim();
        vehicle.FirmName = dto.FirmName;
        vehicle.SeatType = dto.SeatType;
        vehicle.Status = dto.Status;
        repo.Update(vehicle);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var vehicle = await repo.GetByIdAsync(id);
        if (vehicle is null)
        {
            throw new NotFoundException();
        }

        repo.Remove(vehicle);
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

    private static VehicleDto Map(Vehicle v) => new(v.Id, v.Name, v.FirmName, v.SeatType, v.Status);
}
