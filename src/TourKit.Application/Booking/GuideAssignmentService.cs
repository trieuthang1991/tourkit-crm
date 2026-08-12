using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Phân công HDV cho chuyến (legacy TourGuide) — CRUD phân trang, lọc theo chuyến.
/// Cô lập tenant do AppDbContext lo (global filter + interceptor). Validate chuyến + HDV tồn tại khi ghi.
/// </summary>
public sealed class GuideAssignmentService(
    IRepository<TourGuideAssignment> repo,
    IRepository<TourDeparture> departureRepo,
    IRepository<Provider> providerRepo,
    IValidator<CreateGuideAssignmentDto> createValidator,
    IValidator<UpdateGuideAssignmentDto> updateValidator) : IGuideAssignmentService
{
    public async Task<PagedResult<GuideAssignmentDto>> ListAsync(int page, int size, GuideAssignmentListFilter? filter = null)
    {
        var f = filter ?? new GuideAssignmentListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        System.Linq.Expressions.Expression<Func<TourGuideAssignment, bool>> viTu = a =>
            (f.ProviderId == null || a.ProviderId == f.ProviderId) &&
            (f.DepartureId == null || a.TourDepartureId == f.DepartureId) &&
            (f.Status == null || a.Status == f.Status) &&
            (f.DateFrom == null || a.TimeGo >= f.DateFrom) &&
            (f.DateTo == null || a.TimeGo <= f.DateTo);

        // KHÔNG có từ khoá → cắt trang ngay ở SQL, chỉ làm giàu đúng một trang. Chỉ từ khoá mới buộc
        // phải nạp cả tập (nó khớp tên HDV và mã/tên chuyến — nằm ở bảng khác, không dịch được).
        if (kw == null)
        {
            var (trang, tong) = await repo.PageAsync(
                page, size, a => a.TimeGo ?? a.CreatedAt, descending: true, viTu);
            return new PagedResult<GuideAssignmentDto>(await LamGiauAsync(trang), tong, page, size);
        }

        var all = await repo.ListAsync(viTu);

        // Từ khoá: khớp tên HDV / mã-tên chuyến. Các trường này nằm ở bảng khác, join không dịch được
        // sang SQL → nạp tên cho TOÀN BỘ tập đã lọc rồi so khớp ở bộ nhớ (an toàn cho provider InMemory
        // của test), chạy trên tập đã bị các tiêu chí trên thu hẹp.
        IReadOnlyList<TourGuideAssignment> matched = all;
        {
            var pIds = all.Select(a => a.ProviderId).ToHashSet();
            var dIds = all.Select(a => a.TourDepartureId).ToHashSet();
            var pLookup = (await providerRepo.ListAsync(p => pIds.Contains(p.Id))).ToDictionary(p => p.Id, p => p.Name);
            var dLookup = (await departureRepo.ListAsync(d => dIds.Contains(d.Id))).ToDictionary(d => d.Id, d => d);

            bool MatchQ(TourGuideAssignment a)
            {
                var providerName = pLookup.GetValueOrDefault(a.ProviderId);
                dLookup.TryGetValue(a.TourDepartureId, out var dep);
                return (providerName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (dep?.Code?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (dep?.Title?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            matched = all.Where(MatchQ).ToList();
        }

        var ordered = matched.OrderByDescending(a => a.TimeGo ?? a.CreatedAt).ToList();
        var pageItems = ordered.Skip((page - 1) * size).Take(size).ToList();

        return new PagedResult<GuideAssignmentDto>(await LamGiauAsync(pageItems), ordered.Count, page, size);
    }

    /// <summary>
    /// Làm giàu tên HDV + tên/mã chuyến cho ĐÚNG danh sách truyền vào (thường là một trang).
    /// Tách ra để đường nhanh và đường chậm dùng CHUNG — hai bản sao sẽ trôi lệch, và lệch ở đây
    /// nghĩa là hai đường cùng dữ liệu mà hiện ra khác nhau.
    /// </summary>
    private async Task<List<GuideAssignmentDto>> LamGiauAsync(IReadOnlyList<TourGuideAssignment> ds)
    {
        if (ds.Count == 0)
        {
            return [];
        }

        var providerIds = ds.Select(a => a.ProviderId).ToHashSet();
        var departureIds = ds.Select(a => a.TourDepartureId).ToHashSet();
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id)))
            .ToDictionary(p => p.Id, p => p.Name);
        var departures = (await departureRepo.ListAsync(d => departureIds.Contains(d.Id)))
            .ToDictionary(d => d.Id, d => d);

        return ds.Select(a =>
        {
            departures.TryGetValue(a.TourDepartureId, out var dep);
            return Map(a) with
            {
                ProviderName = providerNames.GetValueOrDefault(a.ProviderId),
                DepartureTitle = dep?.Title,
                DepartureCode = dep?.Code,
            };
        }).ToList();
    }

    public async Task<GuideAssignmentStatsDto> GetStatsAsync()
    {
        // Một câu GROUP BY cho mọi bậc trạng thái, thay vì nạp cả bảng hoặc bắn nhiều câu COUNT rời.
        var byStatus = await repo.CountByAsync(a => a.Status);
        // Số HDV khác nhau = số NHÓM của GROUP BY theo ProviderId (tương đương COUNT DISTINCT ở SQL).
        var byProvider = await repo.CountByAsync(a => a.ProviderId);

        return new GuideAssignmentStatsDto(
            byStatus.Values.Sum(),
            byStatus.GetValueOrDefault(1),
            byStatus.GetValueOrDefault(2),
            byProvider.Count);
    }

    public async Task<GuideAssignmentDto> CreateAsync(CreateGuideAssignmentDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureDepartureExists(dto.TourDepartureId);
        await EnsureProviderExists(dto.ProviderId);
        await EnsureNoScheduleConflict(dto.ProviderId, dto.TourDepartureId, excludeId: null);

        var assignment = new TourGuideAssignment
        {
            TourDepartureId = dto.TourDepartureId,
            ProviderId = dto.ProviderId,
            TimeGo = dto.TimeGo,
            TimeCome = dto.TimeCome,
            TimeReturn = dto.TimeReturn,
            Note = dto.Note,
            Status = dto.Status,
        };
        await repo.AddAsync(assignment);
        await repo.SaveChangesAsync();

        return Map(assignment);
    }

    public async Task UpdateAsync(Guid id, UpdateGuideAssignmentDto dto)
    {
        await Validate(updateValidator, dto);
        await EnsureProviderExists(dto.ProviderId);

        var assignment = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        // Chuyến của phân công không đổi khi sửa (UpdateDto không mang TourDepartureId) → lấy từ bản ghi.
        await EnsureNoScheduleConflict(dto.ProviderId, assignment.TourDepartureId, excludeId: id);

        assignment.ProviderId = dto.ProviderId;
        assignment.TimeGo = dto.TimeGo;
        assignment.TimeCome = dto.TimeCome;
        assignment.TimeReturn = dto.TimeReturn;
        assignment.Note = dto.Note;
        assignment.Status = dto.Status;
        repo.Update(assignment);
        await repo.SaveChangesAsync();
    }

    public async Task<GuideAssignmentDto> HandoverAsync(Guid id, HandoverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            throw new ValidationAppException("Cần nội dung bàn giao.");
        }

        var assignment = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        assignment.HandoverContent = dto.Content.Trim();
        assignment.HandedOverAt = DateTimeOffset.UtcNow;
        repo.Update(assignment);
        await repo.SaveChangesAsync();

        return Map(assignment);
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
    /// Chống trùng lịch HDV (legacy CarManagementAction.HandleGuideInTour): CÙNG một HDV không được
    /// gán vào hai chuyến có khoảng ngày (DepartureDate..EndDate) GIAO NHAU. Bỏ qua bản ghi đã xoá
    /// (Status = 4) và bản ghi đang sửa (excludeId). Không xác định được khoảng ngày → bỏ kiểm tra.
    /// </summary>
    private async Task EnsureNoScheduleConflict(Guid providerId, Guid departureId, Guid? excludeId)
    {
        var target = await departureRepo.GetByIdAsync(departureId);
        if (target?.DepartureDate is null)
        {
            return; // chuyến chưa có ngày đi → không có căn cứ xác định trùng
        }

        var targetStart = target.DepartureDate.Value;
        var targetEnd = target.EndDate ?? targetStart;

        // Các phân công đang hiệu lực của CÙNG HDV (Status != 4 = chưa xoá), trừ bản ghi đang sửa.
        var others = (await repo.ListAsync(a => a.ProviderId == providerId && a.Status != 4))
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
                throw new ValidationAppException("Hướng dẫn viên đã có lịch trùng trong khoảng thời gian này.");
            }
        }
    }

    private async Task EnsureProviderExists(Guid providerId)
    {
        if (!await providerRepo.AnyAsync(p => p.Id == providerId))
        {
            throw new ValidationAppException("HDV (provider) không tồn tại.");
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

    private static GuideAssignmentDto Map(TourGuideAssignment a) =>
        new(a.Id, a.TourDepartureId, a.ProviderId, a.TimeGo, a.TimeCome, a.TimeReturn, a.Note, a.Status,
            a.HandoverContent, a.HandedOverAt);
}
