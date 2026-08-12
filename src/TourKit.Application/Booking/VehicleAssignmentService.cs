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
    public async Task<PagedResult<VehicleAssignmentDto>> ListAsync(int page, int size, VehicleAssignmentListFilter? filter = null)
    {
        var f = filter ?? new VehicleAssignmentListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        System.Linq.Expressions.Expression<Func<VehicleAssignment, bool>> viTu = a =>
            (f.VehicleId == null || a.VehicleId == f.VehicleId) &&
            (f.DepartureId == null || a.TourDepartureId == f.DepartureId) &&
            (f.Status == null || a.Status == f.Status) &&
            (f.DateFrom == null || a.TimeGo >= f.DateFrom) &&
            (f.DateTo == null || a.TimeGo <= f.DateTo);

        // KHÔNG có từ khoá → cắt trang ngay ở SQL, chỉ làm giàu đúng một trang.
        if (kw == null)
        {
            var (trang, tong) = await repo.PageAsync(
                page, size, a => a.TimeGo ?? a.CreatedAt, descending: true, viTu);
            return new PagedResult<VehicleAssignmentDto>(await LamGiauAsync(trang), tong, page, size);
        }

        var all = await repo.ListAsync(viTu);

        // Từ khoá: khớp tên xe / mã-tên chuyến / tài xế. Các trường tên xe & chuyến nằm ở bảng khác,
        // join không dịch được sang SQL → nạp tên cho TOÀN BỘ tập đã lọc rồi so khớp ở bộ nhớ
        // (an toàn cho provider InMemory của test), chạy trên tập đã bị các tiêu chí trên thu hẹp.
        IReadOnlyList<VehicleAssignment> matched = all;
        {
            var vIds = all.Select(a => a.VehicleId).ToHashSet();
            var dIds = all.Select(a => a.TourDepartureId).ToHashSet();
            var vLookup = (await vehicleRepo.ListAsync(v => vIds.Contains(v.Id))).ToDictionary(v => v.Id, v => v);
            var dLookup = (await departureRepo.ListAsync(d => dIds.Contains(d.Id))).ToDictionary(d => d.Id, d => d);

            bool MatchQ(VehicleAssignment a)
            {
                vLookup.TryGetValue(a.VehicleId, out var veh);
                dLookup.TryGetValue(a.TourDepartureId, out var dep);
                return (veh?.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (dep?.Code?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (dep?.Title?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (a.DriverName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (a.DriverPhone?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            matched = all.Where(MatchQ).ToList();
        }

        var ordered = matched.OrderByDescending(a => a.TimeGo ?? a.CreatedAt).ToList();
        var pageItems = ordered.Skip((page - 1) * size).Take(size).ToList();

        return new PagedResult<VehicleAssignmentDto>(await LamGiauAsync(pageItems), ordered.Count, page, size);
    }

    /// <summary>
    /// Làm giàu tên xe + tên/mã chuyến cho ĐÚNG danh sách truyền vào. Đường nhanh và đường chậm dùng
    /// CHUNG hàm này — hai bản sao sẽ trôi lệch, mà lệch ở đây nghĩa là cùng dữ liệu hiện ra khác nhau.
    /// </summary>
    private async Task<List<VehicleAssignmentDto>> LamGiauAsync(IReadOnlyList<VehicleAssignment> ds)
    {
        if (ds.Count == 0)
        {
            return [];
        }

        var vehicleIds = ds.Select(a => a.VehicleId).ToHashSet();
        var departureIds = ds.Select(a => a.TourDepartureId).ToHashSet();
        var vehicles = (await vehicleRepo.ListAsync(v => vehicleIds.Contains(v.Id)))
            .ToDictionary(v => v.Id, v => v);
        var departures = (await departureRepo.ListAsync(d => departureIds.Contains(d.Id)))
            .ToDictionary(d => d.Id, d => d);

        return ds.Select(a =>
        {
            vehicles.TryGetValue(a.VehicleId, out var veh);
            departures.TryGetValue(a.TourDepartureId, out var dep);
            return Map(a) with
            {
                VehicleName = veh is null ? null : $"{veh.Name} ({veh.SeatType} chỗ)",
                DepartureTitle = dep?.Title,
                DepartureCode = dep?.Code,
            };
        }).ToList();
    }

    public async Task<VehicleAssignmentStatsDto> GetStatsAsync()
    {
        // Một câu GROUP BY cho mọi bậc trạng thái, thay vì nạp cả bảng hoặc bắn nhiều câu COUNT rời.
        var byStatus = await repo.CountByAsync(a => a.Status);
        // Số xe khác nhau = số NHÓM của GROUP BY theo VehicleId (tương đương COUNT DISTINCT ở SQL).
        var byVehicle = await repo.CountByAsync(a => a.VehicleId);

        return new VehicleAssignmentStatsDto(
            byStatus.Values.Sum(), byStatus.GetValueOrDefault(1), byStatus.GetValueOrDefault(2),
            byVehicle.Count);
    }

    public async Task<VehicleAssignmentDto> CreateAsync(CreateVehicleAssignmentDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureDepartureExists(dto.TourDepartureId);
        await EnsureVehicleExists(dto.VehicleId);
        await EnsureNoScheduleConflict(dto.VehicleId, dto.TourDepartureId, excludeId: null);

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
        // Chuyến của phân công không đổi khi sửa (UpdateDto không mang TourDepartureId) → lấy từ bản ghi.
        await EnsureNoScheduleConflict(dto.VehicleId, assignment.TourDepartureId, excludeId: id);

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

    /// <summary>
    /// Chống trùng lịch xe (song song HDV, legacy CarManagementAction): CÙNG một xe không được gán vào
    /// hai chuyến có khoảng ngày (DepartureDate..EndDate) GIAO NHAU. Bỏ qua bản ghi đã xoá (Status = 4)
    /// và bản ghi đang sửa (excludeId). Không xác định được khoảng ngày → bỏ kiểm tra.
    /// Ghi chú: tài xế chỉ là văn bản (DriverName/DriverPhone), không phải danh mục → không kiểm trùng theo tài xế.
    /// </summary>
    private async Task EnsureNoScheduleConflict(Guid vehicleId, Guid departureId, Guid? excludeId)
    {
        var target = await departureRepo.GetByIdAsync(departureId);
        if (target?.DepartureDate is null)
        {
            return; // chuyến chưa có ngày đi → không có căn cứ xác định trùng
        }

        var targetStart = target.DepartureDate.Value;
        var targetEnd = target.EndDate ?? targetStart;

        // Các phân công đang hiệu lực của CÙNG XE (Status != 4 = chưa xoá), trừ bản ghi đang sửa.
        var others = (await repo.ListAsync(a => a.VehicleId == vehicleId && a.Status != 4))
            .Where(a => excludeId == null || a.Id != excludeId)
            .ToList();
        if (others.Count == 0)
        {
            return;
        }

        var depIds = others.Select(a => a.TourDepartureId).ToHashSet();
        var deps = (await departureRepo.ListAsync(d => depIds.Contains(d.Id)))
            .ToDictionary(d => d.Id, d => d);

        foreach (var other in others)
        {
            if (!deps.TryGetValue(other.TourDepartureId, out var dep) || dep.DepartureDate is null)
            {
                continue;
            }

            var otherStart = dep.DepartureDate.Value;
            var otherEnd = dep.EndDate ?? otherStart;
            // Hai khoảng [s1,e1] và [s2,e2] giao nhau ⇔ s1 <= e2 && s2 <= e1.
            if (targetStart <= otherEnd && otherStart <= targetEnd)
            {
                throw new ValidationAppException("Xe đã có lịch trùng trong khoảng thời gian này.");
            }
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
