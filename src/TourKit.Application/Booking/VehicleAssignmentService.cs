using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Phân xe cho chuyến (điều hành) — CRUD phân trang, lọc theo chuyến. Song song GuideAssignmentService.
/// Cô lập tenant do AppDbContext lo (global filter + interceptor). Validate chuyến + xe tồn tại khi ghi.
/// </summary>
public sealed class VehicleAssignmentService(
    IRepository<VehicleAssignment> repo,
    IRepository<TourDeparture> departureRepo,
    IRepository<Vehicle> vehicleRepo,
    IValidator<CreateVehicleAssignmentDto> createValidator,
    IValidator<UpdateVehicleAssignmentDto> updateValidator) : IVehicleAssignmentService
{
    public async Task<PagedResult<VehicleAssignmentDto>> ListAsync(int page, int size, Guid? departureId)
    {
        var (items, total) = departureId is { } depId
            ? await repo.PageAsync(page, size, a => a.TourDepartureId == depId)
            : await repo.PageAsync(page, size);
        return new PagedResult<VehicleAssignmentDto>(items.Select(Map).ToList(), total, page, size);
    }

    public async Task<VehicleAssignmentDto> CreateAsync(CreateVehicleAssignmentDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureDepartureExists(dto.TourDepartureId);
        await EnsureVehicleExists(dto.VehicleId);

        var assignment = new VehicleAssignment
        {
            TourDepartureId = dto.TourDepartureId,
            VehicleId = dto.VehicleId,
            DriverName = dto.DriverName,
            DriverPhone = dto.DriverPhone,
            TimeGo = dto.TimeGo,
            TimeCome = dto.TimeCome,
            Note = dto.Note,
            Status = dto.Status,
        };
        await repo.AddAsync(assignment);
        await repo.SaveChangesAsync();

        return Map(assignment);
    }

    public async Task UpdateAsync(Guid id, UpdateVehicleAssignmentDto dto)
    {
        await Validate(updateValidator, dto);
        await EnsureVehicleExists(dto.VehicleId);

        var assignment = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        assignment.VehicleId = dto.VehicleId;
        assignment.DriverName = dto.DriverName;
        assignment.DriverPhone = dto.DriverPhone;
        assignment.TimeGo = dto.TimeGo;
        assignment.TimeCome = dto.TimeCome;
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

    private async Task EnsureVehicleExists(Guid vehicleId)
    {
        if (!await vehicleRepo.AnyAsync(v => v.Id == vehicleId))
        {
            throw new ValidationAppException("Xe (vehicle) không tồn tại.");
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

    private static VehicleAssignmentDto Map(VehicleAssignment a) =>
        new(a.Id, a.TourDepartureId, a.VehicleId, a.DriverName, a.DriverPhone, a.TimeGo, a.TimeCome, a.Note, a.Status);
}
