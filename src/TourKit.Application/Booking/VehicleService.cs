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
    public async Task<PagedResult<VehicleDto>> ListAsync(int page, int size, string? q = null)
    {
        var kw = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        // Không từ khoá → cắt trang NGAY Ở SQL (CreatedAt giảm dần), không nạp cả bảng.
        if (kw == null)
        {
            var (items, total) = await repo.PageAsync(page, size);
            return new PagedResult<VehicleDto>(items.Select(Map).ToList(), total, page, size);
        }

        // Có từ khoá: khớp tên xe + hãng (xe không có biển số/mã ở entity). EF không dịch được
        // StringComparison nên lọc ở bộ nhớ (LINQ-to-objects, an toàn cho provider InMemory của test).
        var all = await repo.ListAsync();
        bool MatchQ(Vehicle v) =>
            v.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            (v.FirmName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);

        var filtered = all.Where(MatchQ).OrderByDescending(v => v.CreatedAt).ToList();
        var dtos = filtered.Skip((page - 1) * size).Take(size).Select(Map).ToList();
        return new PagedResult<VehicleDto>(dtos, filtered.Count, page, size);
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
