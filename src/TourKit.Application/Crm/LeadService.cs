using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Crm;

/// <summary>
/// Lead (khách tiềm năng, phễu bán). Convert đánh dấu lead Won + gắn ConvertedCustomerId — chỉ
/// convert được 1 lần (convert lại → Conflict).
///
/// Hồ sơ <see cref="Customer"/> đi qua <c>ICustomerService</c>, KHÔNG dựng thẳng: số điện thoại đã
/// có chủ thì NỐI vào hồ sơ sẵn có, chưa có mới tạo. Xem chú thích trong <c>ConvertAsync</c>.
/// </summary>
public sealed class LeadService(
    IRepository<Lead> repo,
    ICustomerService customers,
    ILeadCampaignService campaigns,
    IValidator<CreateLeadDto> createValidator,
    IValidator<UpdateLeadDto> updateValidator,
    TourKit.Shared.Security.ICurrentUserContext currentUser) : ILeadService
{
    public async Task<PagedResult<LeadDto>> ListAsync(int page, int size, LeadListFilter? filter = null)
    {
        var f = filter ?? new LeadListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();
        var src = string.IsNullOrWhiteSpace(f.Source) ? null : f.Source.Trim();
        var (items, total) = await repo.PageAsync(page, size, l =>
            (f.Status == null || (int)l.Status == f.Status) &&
            (f.AssignedToUserId == null || l.AssignedToUserId == f.AssignedToUserId) &&
            (f.BranchId == null || l.BranchId == f.BranchId) &&
            (f.CreatedByUserId == null || l.CreatedByUserId == f.CreatedByUserId) &&
            (f.CampaignId == null || l.CampaignId == f.CampaignId) &&
            (src == null || (l.Source != null && l.Source.Contains(src))) &&
            (f.CreatedFrom == null || l.CreatedAt >= f.CreatedFrom) &&
            (f.CreatedTo == null || l.CreatedAt <= f.CreatedTo) &&
            (kw == null ||
                l.FullName.Contains(kw) ||
                (l.Phone != null && l.Phone.Contains(kw)) ||
                (l.Email != null && l.Email.Contains(kw)) ||
                (l.Source != null && l.Source.Contains(kw))));
        var dtos = items.Select(Map).ToList();
        return new PagedResult<LeadDto>(dtos, total, page, size);
    }

    public async Task<LeadFilterOptionsDto> GetFilterOptionsAsync()
    {
        var all = await repo.ListAsync();
        var sources = all
            .Where(l => !string.IsNullOrWhiteSpace(l.Source))
            .Select(l => l.Source!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.CurrentCulture)
            .ToList();
        return new LeadFilterOptionsDto(sources);
    }

    /// <summary>
    /// Đếm bằng COUNT ở SQL, mỗi bậc một truy vấn nhẹ — thay vì kéo cả bảng Cơ hội (3.000 dòng)
    /// về rồi đếm 7 lần trong bộ nhớ. Thẻ thống kê này chạy cùng MỌI lần mở màn Cơ hội.
    /// </summary>
    public async Task<LeadStatsDto> GetStatsAsync() => new(
        await repo.CountAsync(),
        await repo.CountAsync(l => l.Status == LeadStatus.New),
        await repo.CountAsync(l => l.Status == LeadStatus.Contacted),
        await repo.CountAsync(l => l.Status == LeadStatus.Qualified),
        await repo.CountAsync(l => l.Status == LeadStatus.Won),
        await repo.CountAsync(l => l.Status == LeadStatus.Lost),
        await repo.CountAsync(l => l.ConvertedCustomerId != null));

    public async Task<LeadDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<LeadDto?> FindByConvertedCustomerAsync(Guid customerId)
    {
        // Vị từ đi xuống SQL và chỉ về tối đa một dòng (mỗi lead chuyển đúng một lần) — không nạp
        // bảng rồi lọc trong bộ nhớ.
        var found = await repo.ListAsync(l => l.ConvertedCustomerId == customerId);
        return found.Count == 0 ? null : Map(found[0]);
    }

    public async Task<LeadDto> CreateAsync(CreateLeadDto dto)
    {
        await Validate(createValidator, dto);

        // Form thu lead gửi MÃ chiến dịch (CD-2026-007), không gửi khoá — người dựng form chép được
        // cái mã, chứ chép một GUID là cầm chắc sai một ký tự mà không ai phát hiện ra.
        //
        // Chiến dịch có bật chia số thì lấy luôn người phụ trách từ đó. Nhưng lời gọi đã CHỈ ĐỊNH
        // người thì tôn trọng — chia tự động là để lấp chỗ trống, không phải để đè lên quyết định
        // của người dùng.
        var chiaSo = await campaigns.ChiaSoAsync(dto.CampaignCode);

        var entity = new Lead
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone,
            Email = dto.Email,
            Source = dto.Source,
            AssignedToUserId = dto.AssignedToUserId ?? chiaSo?.AssignedToUserId,
            BranchId = dto.BranchId,
            CreatedByUserId = currentUser.UserId,
            Note = dto.Note,
            AttributionJson = dto.Attribution?.ToJsonOrNull(),
            CampaignId = dto.CampaignId ?? chiaSo?.CampaignId,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateLeadDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.FullName = dto.FullName.Trim();
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Source = dto.Source;
        entity.Status = dto.Status;
        entity.AssignedToUserId = dto.AssignedToUserId;
        entity.BranchId = dto.BranchId;
        entity.Note = dto.Note;

        // Không gửi phần nguồn chi tiết thì GIỮ NGUYÊN cái đang có. Nguồn chi tiết do form thu lead
        // ghi lúc khách để lại thông tin; một lần sửa tay trong CRM không được phép xoá dấu vết đó.
        if (dto.Attribution is not null)
        {
            entity.AttributionJson = dto.Attribution.ToJsonOrNull();
        }

        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    public async Task<ConvertLeadResultDto> ConvertAsync(Guid id, Guid? assignedToUserId = null)
    {
        var lead = await repo.GetByIdAsync(id);
        if (lead is null)
        {
            throw new NotFoundException();
        }

        if (lead.ConvertedCustomerId is not null)
        {
            throw new ConflictException("Lead đã được convert.");
        }

        // Đi QUA CustomerService, không ghi thẳng kho.
        //
        // Bản trước dựng `new Customer{...}` rồi AddAsync, nên bỏ qua cả ba thứ chỉ có trong
        // CustomerService: sinh mã KH_, dựng SearchName (tìm không dấu) và PhoneNormalized, và
        // chặn trùng số điện thoại. Khách sinh ra vì vậy TÌM KHÔNG RA ở màn Data khách hàng, mà
        // cũng không lọt vào màn Rà khách trùng — màn đó gom theo đúng cột PhoneNormalized đang
        // trống. Tách hai bảng là để bảng khách hàng sạch hơn bảng lead; ghi tắt thế này thì chính
        // đường chuyển đổi chọc thủng cái ranh giới ấy.
        //
        // Số đã thuộc về một khách sẵn có thì GẮN vào khách đó: lead này chính là người ấy. Ném lỗi
        // thì người bán bị chặn mà không có lối đi tiếp, còn tạo thêm hồ sơ nữa thì đúng là thứ luật
        // chặn trùng sinh ra để ngăn.
        var sanCo = await customers.FindByPhoneAsync(lead.Phone);
        var customerId = sanCo?.Id;

        if (customerId is null)
        {
            // Mang theo nguồn và email — hai thứ đã biết về khách; bỏ lại là mất dữ liệu ngay tại
            // bước chuyển đổi, rồi không còn đường nào lấy lại.
            // Người phụ trách ĐI THEO sang khách hàng. Người bấm nút chọn ở hộp xác nhận, mặc định
            // là người đang phụ trách lead — bàn giao là việc có chủ ý, không nên xảy ra âm thầm.
            var phuTrach = assignedToUserId ?? lead.AssignedToUserId;

            var moi = await customers.CreateAsync(new CreateCustomerDto(
                lead.FullName, lead.Phone, Source: lead.Source, Email: lead.Email,
                InitialNeed: lead.Note,     // nhu cầu khách nêu lúc còn là lead → "nhu cầu ban đầu"
                AssignedTo: phuTrach is { } pt ? [pt.ToString()] : null));
            customerId = moi.Id;
        }

        lead.Status = LeadStatus.Won;
        lead.ConvertedCustomerId = customerId;
        repo.Update(lead);
        await repo.SaveChangesAsync();

        return new ConvertLeadResultDto(customerId.Value, sanCo is not null);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static LeadDto Map(Lead l) => new(
        l.Id, l.FullName, l.Phone, l.Email, l.Source, l.Status, l.AssignedToUserId, l.ConvertedCustomerId, l.BranchId,
        l.Note, LeadAttribution.Parse(l.AttributionJson));
}
