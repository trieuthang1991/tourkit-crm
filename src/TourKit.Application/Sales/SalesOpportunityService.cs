using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Sales;

/// <summary>
/// Cơ hội bán hàng — xem <see cref="ISalesOpportunityService"/>.
///
/// Hai luật nghiệp vụ nằm ở <see cref="MoveStageAsync"/>, KHÔNG ở màn hình: chặn phía giao diện thì
/// mọi đường ghi khác (API, nhập liệu hàng loạt, sửa nhanh trên kanban) đều lách được.
/// </summary>
public sealed class SalesOpportunityService(
    IRepository<SalesOpportunity> repo,
    IRepository<SalesOpportunityAssignee> assigneeRepo,
    IRepository<OpportunityStage> stageRepo,
    IRepository<TransferReason> reasonRepo,
    IValidator<CreateSalesOpportunityDto> createValidator,
    IValidator<UpdateSalesOpportunityDto> updateValidator,
    TourKit.Shared.Security.ICurrentUserContext currentUser) : ISalesOpportunityService
{
    public async Task<PagedResult<SalesOpportunityDto>> ListAsync(
        int page, int size, SalesOpportunityListFilter? filter = null)
    {
        var f = filter ?? new SalesOpportunityListFilter();
        var loc = await DungBoLoc(f);

        // Cơ hội mới nhất lên đầu: màn này để xử lý việc đang tới, không phải để tra sổ.
        var (items, total) = await repo.PageAsync(page, size, o => o.CreatedAt, descending: true, loc);

        // Nạp người phụ trách của ĐÚNG trang hiện tại bằng MỘT câu, không phải mỗi dòng một câu.
        var ids = items.Select(o => o.Id).ToList();
        var nguoi = ids.Count == 0
            ? []
            : await assigneeRepo.ListAsync(a => ids.Contains(a.OpportunityId));

        var theoCoHoi = nguoi.GroupBy(a => a.OpportunityId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<OpportunityAssigneeDto>)
                g.Select(a => new OpportunityAssigneeDto(a.UserId, a.IsFollower)).ToList());

        var dtos = items.Select(o => Map(o, theoCoHoi.GetValueOrDefault(o.Id, []))).ToList();
        return new PagedResult<SalesOpportunityDto>(dtos, total, page, size);
    }

    public async Task<SalesOpportunityStatsDto> GetStatsAsync(SalesOpportunityListFilter? filter = null)
    {
        var f = filter ?? new SalesOpportunityListFilter();
        var loc = await DungBoLoc(f);

        // Đếm theo cột bằng MỘT câu GROUP BY, không bắn mỗi cột một COUNT: số cột do người dùng cấu
        // hình nên không biết trước có bao nhiêu — bắn theo cột là số câu truy vấn tăng theo cấu hình.
        var theoCot = await repo.CountByAsync(o => o.StageCode, loc);

        // Cộng ở SQL qua biểu thức dùng chung, không nạp bảng về rồi cộng trong bộ nhớ.
        var tong = await repo.SumAsync(OpportunityMath.GiaTriSelector, loc);

        // "Đang mở" = chưa chốt và chưa huỷ. Đây mới là con số người bán nhìn để biết còn bao nhiêu
        // tiền đang chạy; tổng gộp cả đơn đã huỷ thì luôn đẹp và luôn vô nghĩa.
        var dangMoLoc = Ghep(loc, o =>
            o.StageCode != OpportunityStageCode.ChotDon && o.StageCode != OpportunityStageCode.Huy);
        var giaTriDangMo = await repo.SumAsync(OpportunityMath.GiaTriSelector, dangMoLoc);

        return new SalesOpportunityStatsDto(
            theoCot.Values.Sum(),
            tong,
            giaTriDangMo,
            theoCot.GetValueOrDefault(OpportunityStageCode.ChotDon),
            theoCot.GetValueOrDefault(OpportunityStageCode.Huy),
            theoCot);
    }

    public async Task<SalesOpportunityDto> GetAsync(Guid id)
    {
        var e = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        return Map(e, await NguoiCua(id));
    }

    public async Task<SalesOpportunityDto> CreateAsync(CreateSalesOpportunityDto dto)
    {
        await Validate(createValidator, dto);
        await ChongTrungMa(dto.Code, null);

        var e = new SalesOpportunity
        {
            Code = dto.Code.Trim(),
            Title = dto.Title.Trim(),
            Content = dto.Content,
            ContactName = dto.ContactName.Trim(),
            ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail,
            ContactAddress = dto.ContactAddress,
            CustomerId = dto.CustomerId,
            AdultQty = dto.AdultQty,
            ChildQty = dto.ChildQty,
            ChildSmallQty = dto.ChildSmallQty,
            BabyQty = dto.BabyQty,
            PriceAdult = dto.PriceAdult,
            PriceChild = dto.PriceChild,
            PriceChildSmall = dto.PriceChildSmall,
            PriceBaby = dto.PriceBaby,
            TemplateId = dto.TemplateId,
            TourDepartureId = dto.TourDepartureId,
            StageCode = OpportunityStageCode.TaoMoi,
            BranchId = dto.BranchId,
            CustomerSourceId = dto.CustomerSourceId,
            MarketTypeId = dto.MarketTypeId,
            FromWebsite = dto.FromWebsite,
            AttachmentIds = dto.AttachmentIds,
            CreatedByUserId = currentUser.UserId,
        };

        await repo.AddAsync(e);
        await repo.SaveChangesAsync();
        await GhiNguoiPhuTrach(e.Id, dto.Assignees);

        return Map(e, await NguoiCua(e.Id));
    }

    public async Task<SalesOpportunityDto> UpdateAsync(Guid id, UpdateSalesOpportunityDto dto)
    {
        await Validate(updateValidator, dto);

        var e = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        await ChongTrungMa(dto.Code, id);

        // Cơ hội đã chốt thành đơn thì khoá: số khách và giá ở đây đã đi vào đơn hàng, sửa ngược lại
        // làm hai bên lệch nhau mà không có gì đối chiếu.
        if (e.ConvertedOrderId is not null)
        {
            throw new ConflictException("Cơ hội đã chốt thành đơn, không sửa được nữa.");
        }

        e.Code = dto.Code.Trim();
        e.Title = dto.Title.Trim();
        e.Content = dto.Content;
        e.ContactName = dto.ContactName.Trim();
        e.ContactPhone = dto.ContactPhone;
        e.ContactEmail = dto.ContactEmail;
        e.ContactAddress = dto.ContactAddress;
        e.CustomerId = dto.CustomerId;
        e.AdultQty = dto.AdultQty;
        e.ChildQty = dto.ChildQty;
        e.ChildSmallQty = dto.ChildSmallQty;
        e.BabyQty = dto.BabyQty;
        e.PriceAdult = dto.PriceAdult;
        e.PriceChild = dto.PriceChild;
        e.PriceChildSmall = dto.PriceChildSmall;
        e.PriceBaby = dto.PriceBaby;
        e.TemplateId = dto.TemplateId;
        e.TourDepartureId = dto.TourDepartureId;
        e.BranchId = dto.BranchId;
        e.CustomerSourceId = dto.CustomerSourceId;
        e.MarketTypeId = dto.MarketTypeId;
        e.FromWebsite = dto.FromWebsite;
        e.AttachmentIds = dto.AttachmentIds;

        repo.Update(e);
        await repo.SaveChangesAsync();
        await GhiNguoiPhuTrach(id, dto.Assignees);

        return Map(e, await NguoiCua(id));
    }

    public async Task DeleteAsync(Guid id)
    {
        var e = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        if (e.ConvertedOrderId is not null)
        {
            throw new ConflictException("Cơ hội đã chốt thành đơn, không xoá được.");
        }

        repo.Remove(e);
        await repo.SaveChangesAsync();
    }

    public async Task<SalesOpportunityDto> MoveStageAsync(Guid id, MoveOpportunityStageDto dto)
    {
        var e = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        // Chốt đơn KHÔNG đặt tay. Hệ cũ đánh dấu cột này từ trong luồng đặt khách lên tour, ngay sau
        // khi đơn thật đã được tạo (uspInsertTourSampleCustomer_V4). Cho kéo thẻ sang "Chốt đơn"
        // trên kanban là tạo ra cơ hội đã chốt mà không có đơn nào phía sau — con số doanh thu dự
        // kiến đẹp lên trong khi không có gì để thu.
        if (dto.StageCode == OpportunityStageCode.ChotDon)
        {
            throw new ConflictException(
                "Cơ hội chuyển sang Chốt đơn khi tạo đơn từ chính cơ hội đó, không đặt tay được.");
        }

        if (e.ConvertedOrderId is not null)
        {
            throw new ConflictException("Cơ hội đã chốt thành đơn, không chuyển cột được nữa.");
        }

        var cot = await stageRepo.AnyAsync(s => s.Code == dto.StageCode);
        if (!cot)
        {
            throw new ValidationAppException("Cột phễu không tồn tại.");
        }

        // Huỷ BẮT BUỘC có lý do — đây là nguồn của báo cáo thống kê lý do huỷ. Không bắt buộc thì ô
        // đó bỏ trống gần hết và báo cáo thành vô dụng, đúng như hệ cũ đã phải dựng riêng một hộp
        // thoại chặn ở bước này.
        if (dto.StageCode == OpportunityStageCode.Huy)
        {
            if (dto.CancelReasonId is null)
            {
                throw new ValidationAppException("Huỷ cơ hội phải chọn lý do.");
            }

            if (!await reasonRepo.AnyAsync(r => r.Id == dto.CancelReasonId.Value))
            {
                throw new ValidationAppException("Lý do huỷ không tồn tại.");
            }

            e.CancelReasonId = dto.CancelReasonId;
            e.CancelNote = dto.CancelNote;
        }
        else
        {
            // Chuyển ra khỏi Huỷ thì bỏ lý do cũ, không để lại lý do treo trên một cơ hội đang chạy.
            e.CancelReasonId = null;
            e.CancelNote = null;
        }

        e.StageCode = dto.StageCode;
        repo.Update(e);
        await repo.SaveChangesAsync();

        return Map(e, await NguoiCua(id));
    }

    public async Task<IReadOnlyList<OpportunityStageDto>> ListStagesAsync()
    {
        var ds = await stageRepo.ListAsync();
        return ds.OrderBy(s => s.SortOrder).ThenBy(s => s.Code)
            .Select(s => new OpportunityStageDto(s.Id, s.Code, s.Name, s.SortOrder, s.Color, s.IsSystem))
            .ToList();
    }

    public async Task<IReadOnlyList<OpportunityByUserRowDto>> ReportByUserAsync(
        DateTimeOffset? tu, DateTimeOffset? den)
    {
        // Báo cáo theo KỲ: nạp đúng khoảng ngày rồi gom trong bộ nhớ.
        //
        // Không đẩy được xuống SQL vì phép gom đi qua BẢNG NỐI người phụ trách và một cơ hội đếm cho
        // NHIỀU người — với IRepository hiện có thì không diễn đạt được câu JOIN đó. Bù lại, ràng
        // buộc khoảng ngày vẫn nằm ở SQL nên lượng nạp về bị chặn theo kỳ, không phải cả bảng.
        var ds = await repo.ListAsync(o =>
            (tu == null || o.CreatedAt >= tu) && (den == null || o.CreatedAt <= den));
        if (ds.Count == 0)
        {
            return [];
        }

        var ids = ds.Select(o => o.Id).ToHashSet();
        var phanCong = (await assigneeRepo.ListAsync(a => !a.IsFollower && ids.Contains(a.OpportunityId)))
            .ToList();

        var theoCoHoi = ds.ToDictionary(o => o.Id);

        return phanCong
            .GroupBy(a => a.UserId)
            .Select(g =>
            {
                var cua = g.Select(a => theoCoHoi[a.OpportunityId]).ToList();
                var chot = cua.Count(o => o.StageCode == OpportunityStageCode.ChotDon);
                var huy = cua.Count(o => o.StageCode == OpportunityStageCode.Huy);
                var mo = cua.Count - chot - huy;

                return new OpportunityByUserRowDto(
                    g.Key,
                    string.Empty,   // tên do tầng giao diện tra từ danh bạ (cache theo tenant)
                    cua.Count, chot, huy, mo,
                    cua.Where(o => o.StageCode != OpportunityStageCode.ChotDon
                                && o.StageCode != OpportunityStageCode.Huy)
                       .Sum(OpportunityMath.GiaTri),
                    cua.Where(o => o.StageCode == OpportunityStageCode.ChotDon).Sum(OpportunityMath.GiaTri),
                    cua.Count == 0 ? 0d : Math.Round(chot * 100d / cua.Count, 1));
            })
            .OrderByDescending(r => r.DaChot)
            .ThenByDescending(r => r.Tong)
            .ToList();
    }

    public async Task<IReadOnlyList<OpportunityCancelReasonRowDto>> ReportCancelReasonsAsync(
        DateTimeOffset? tu, DateTimeOffset? den)
    {
        var huy = await repo.ListAsync(o =>
            o.StageCode == OpportunityStageCode.Huy &&
            (tu == null || o.CreatedAt >= tu) && (den == null || o.CreatedAt <= den));
        if (huy.Count == 0)
        {
            return [];
        }

        var ten = (await reasonRepo.ListAsync()).ToDictionary(r => r.Id, r => r.Name);

        return huy
            .GroupBy(o => o.CancelReasonId)
            .Select(g => new OpportunityCancelReasonRowDto(
                g.Key,
                // Lý do là BẮT BUỘC khi huỷ, nên nhóm "không rõ" chỉ chứa dữ liệu có từ trước luật
                // hoặc lý do đã bị xoá khỏi danh mục. Vẫn hiện ra chứ không giấu: giấu đi thì tổng
                // của báo cáo không khớp số cơ hội đã huỷ và không ai giải thích được phần chênh.
                g.Key is { } id && ten.TryGetValue(id, out var n) ? n : "Không rõ lý do",
                g.Count(),
                g.Sum(OpportunityMath.GiaTri)))
            .OrderByDescending(r => r.GiaTriMat)
            .ToList();
    }

    // ---- riêng tư ----

    /// <summary>
    /// Dựng vị từ lọc, ĐẨY HẾT xuống SQL.
    ///
    /// Lọc theo người phụ trách phải hỏi bảng con trước rồi mới lọc theo tập id — vẫn là hai câu
    /// SQL, khác hẳn cách hệ cũ phải quét LIKE trên cột chuỗi. Tập id ở đây bị chặn tự nhiên bởi số
    /// cơ hội một người đang giữ.
    /// </summary>
    private async Task<System.Linq.Expressions.Expression<Func<SalesOpportunity, bool>>> DungBoLoc(
        SalesOpportunityListFilter f)
    {
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        List<Guid>? theoNguoi = null;
        if (f.AssigneeUserId is { } uid)
        {
            theoNguoi = (await assigneeRepo.ListAsync(a => a.UserId == uid))
                .Select(a => a.OpportunityId).Distinct().ToList();
        }

        return o =>
            (f.StageCode == null || o.StageCode == f.StageCode) &&
            (f.CustomerId == null || o.CustomerId == f.CustomerId) &&
            (f.TemplateId == null || o.TemplateId == f.TemplateId) &&
            (f.CustomerSourceId == null || o.CustomerSourceId == f.CustomerSourceId) &&
            (f.MarketTypeId == null || o.MarketTypeId == f.MarketTypeId) &&
            (f.BranchId == null || o.BranchId == f.BranchId) &&
            (f.CreatedByUserId == null || o.CreatedByUserId == f.CreatedByUserId) &&
            (f.FromWebsite == null || o.FromWebsite == f.FromWebsite) &&
            (f.IsConfirmed == null || o.IsConfirmed == f.IsConfirmed) &&
            (f.CreatedFrom == null || o.CreatedAt >= f.CreatedFrom) &&
            (f.CreatedTo == null || o.CreatedAt <= f.CreatedTo) &&
            (theoNguoi == null || theoNguoi.Contains(o.Id)) &&
            (kw == null ||
                o.Code.Contains(kw) ||
                o.Title.Contains(kw) ||
                o.ContactName.Contains(kw) ||
                (o.ContactPhone != null && o.ContactPhone.Contains(kw)));
    }

    private static System.Linq.Expressions.Expression<Func<SalesOpportunity, bool>> Ghep(
        System.Linq.Expressions.Expression<Func<SalesOpportunity, bool>> a,
        System.Linq.Expressions.Expression<Func<SalesOpportunity, bool>> b)
    {
        var p = System.Linq.Expressions.Expression.Parameter(typeof(SalesOpportunity), "o");
        var than = System.Linq.Expressions.Expression.AndAlso(
            new ThayThamSo(a.Parameters[0], p).Visit(a.Body)!,
            new ThayThamSo(b.Parameters[0], p).Visit(b.Body)!);
        return System.Linq.Expressions.Expression.Lambda<Func<SalesOpportunity, bool>>(than, p);
    }

    /// <summary>Đổi tham số của một biểu thức để ghép AND được hai lambda rời nhau.</summary>
    private sealed class ThayThamSo(System.Linq.Expressions.Expression cu, System.Linq.Expressions.Expression moi)
        : System.Linq.Expressions.ExpressionVisitor
    {
        protected override System.Linq.Expressions.Expression VisitParameter(
            System.Linq.Expressions.ParameterExpression node) => node == cu ? moi : base.VisitParameter(node);
    }

    private async Task ChongTrungMa(string code, Guid? boQua)
    {
        var ma = code.Trim();
        var trung = await repo.AnyAsync(o => o.Code == ma && (boQua == null || o.Id != boQua));
        if (trung)
        {
            throw new ConflictException($"Mã cơ hội \"{ma}\" đã tồn tại.");
        }
    }

    private async Task<IReadOnlyList<OpportunityAssigneeDto>> NguoiCua(Guid id)
    {
        var ds = await assigneeRepo.ListAsync(a => a.OpportunityId == id);
        return ds.Select(a => new OpportunityAssigneeDto(a.UserId, a.IsFollower)).ToList();
    }

    /// <summary>Ghi lại danh sách người phụ trách: xoá hết rồi thêm lại đúng danh sách gửi lên.</summary>
    private async Task GhiNguoiPhuTrach(Guid id, IReadOnlyList<OpportunityAssigneeDto>? ds)
    {
        if (ds is null)
        {
            return;   // null = không đụng tới; danh sách rỗng = xoá hết.
        }

        foreach (var cu in await assigneeRepo.ListAsync(a => a.OpportunityId == id))
        {
            assigneeRepo.Remove(cu);
        }

        // Lọc trùng ngay tại đây: gửi lên hai lần cùng một người cùng một vai sẽ đụng ràng buộc duy
        // nhất ở CSDL và ném ra lỗi khó hiểu, trong khi ý người dùng chỉ là "gán người này".
        foreach (var a in ds.DistinctBy(x => (x.UserId, x.IsFollower)))
        {
            await assigneeRepo.AddAsync(new SalesOpportunityAssignee
            {
                OpportunityId = id,
                UserId = a.UserId,
                IsFollower = a.IsFollower,
            });
        }

        await assigneeRepo.SaveChangesAsync();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var kq = await validator.ValidateAsync(dto);
        if (!kq.IsValid)
        {
            throw new ValidationAppException(kq.Errors[0].ErrorMessage);
        }
    }

    private static SalesOpportunityDto Map(SalesOpportunity o, IReadOnlyList<OpportunityAssigneeDto> nguoi) => new(
        o.Id, o.Code, o.Title, o.Content,
        o.ContactName, o.ContactPhone, o.ContactEmail, o.ContactAddress,
        o.CustomerId,
        o.AdultQty, o.ChildQty, o.ChildSmallQty, o.BabyQty,
        o.PriceAdult, o.PriceChild, o.PriceChildSmall, o.PriceBaby,
        OpportunityMath.GiaTri(o),
        o.TemplateId, o.TourDepartureId,
        o.StageCode,
        o.CancelReasonId, o.CancelNote,
        o.ConvertedOrderId,
        nguoi,
        o.CreatedByUserId, o.BranchId,
        o.CustomerSourceId, o.MarketTypeId, o.FromWebsite,
        o.IsConfirmed, o.ConfirmedAt, o.ConfirmedByUserId,
        o.AttachmentIds,
        o.CreatedAt);
}
