using System.Globalization;
using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Crm;

/// <summary>
/// Chiến dịch chia số Sale (legacy "Chia số Sale"): gom lead vào chiến dịch, theo dõi tiến độ chăm sóc
/// + tỷ lệ chốt. Số liệu mỗi chiến dịch tính từ Lead (CampaignId): tổng · đã chăm sóc (Status ≠ New) ·
/// đã chốt (Won) · tiến độ (đã xử lý/tổng) · tỷ lệ chốt (Won/tổng).
/// </summary>
public sealed class LeadCampaignService(
    IRepository<LeadCampaign> repo,
    IRepository<Lead> leadRepo,
    IRepository<User> userRepo) : ILeadCampaignService
{
    public async Task<PagedResult<LeadCampaignDto>> ListAsync(int page, int size, LeadCampaignListFilter? filter = null)
    {
        var f = filter ?? new LeadCampaignListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        // Hai tiêu chí đều là cột của chính bảng → cắt trang thẳng ở SQL.
        var kwL = kw?.ToLowerInvariant();
#pragma warning disable CA1304, CA1311, CA1862
        var (pageItems, tong) = await repo.PageAsync(page, size, c => c.CreatedAt, descending: true, c =>
            (f.CreatedByUserId == null || c.CreatedByUserId == f.CreatedByUserId) &&
            (kwL == null || c.Name.ToLower().Contains(kwL)));
#pragma warning restore CA1304, CA1311, CA1862

        var leadsByCampaign = (await leadRepo.ListAsync(l => l.CampaignId != null))
            .GroupBy(l => l.CampaignId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var userNames = (await userRepo.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);

        var dtos = pageItems.Select(c => Map(c, leadsByCampaign.GetValueOrDefault(c.Id, []), userNames)).ToList();
        return new PagedResult<LeadCampaignDto>(dtos, tong, page, size);
    }

    public async Task<LeadCampaignStatsDto> GetStatsAsync()
    {
        // Một câu GROUP BY cho mọi bậc trạng thái, thay vì nạp cả bảng hoặc bắn nhiều câu COUNT rời.
        var byCampaignStatus = await repo.CountByAsync(c => c.Status);
        var total = await leadRepo.CountAsync(l => l.CampaignId != null);
        var won = await leadRepo.CountAsync(l => l.CampaignId != null && l.Status == LeadStatus.Won);
        var avgClose = total == 0 ? 0m : Math.Round(won * 100m / total, 2);

        return new LeadCampaignStatsDto(
            byCampaignStatus.Values.Sum(), total, avgClose, byCampaignStatus.GetValueOrDefault(1));
    }

    public async Task<LeadCampaignDto> CreateAsync(CreateLeadCampaignDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationAppException("Tên chiến dịch bắt buộc.");
        }

        var entity = new LeadCampaign
        {
            Name = dto.Name.Trim(),
            Note = dto.Note,
            Status = 0,
            Code = await SinhMaAsync(),
            AssignMode = dto.AssignMode,
            AssigneesJson = NhomChiaSo.ToJsonOrNull(dto.Assignees),
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        var userNames = (await userRepo.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
        return Map(entity, [], userNames);
    }

    public async Task<LeadCampaignDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var leads = (await leadRepo.ListAsync(l => l.CampaignId == id)).ToList();
        var userNames = (await userRepo.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
        return Map(entity, leads, userNames);
    }

    public async Task UpdateAsync(Guid id, UpdateLeadCampaignDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationAppException("Tên chiến dịch bắt buộc.");
        }

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        entity.Name = dto.Name.Trim();
        entity.Note = dto.Note;
        entity.AssignMode = dto.AssignMode;
        entity.AssigneesJson = NhomChiaSo.ToJsonOrNull(dto.Assignees);

        // Mã KHÔNG đổi được: nó đã nằm trong form thu lead của khách, đổi ở đây là làm chết một
        // đường dẫn đang chạy mà không ai biết cho tới khi hết lead.
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    /// <summary>Đổi nhanh trạng thái Đang chạy(0)/Hoàn thành(1) — xem <see cref="ILeadCampaignService.SetStatusAsync"/>.</summary>
    public async Task SetStatusAsync(Guid id, int status)
    {
        // Kiểm duyệt chỉ có 2 trạng thái — chặn giá trị lạ ngay ở biên, không để lọt số bừa vào DB.
        if (status is not (0 or 1))
        {
            throw new ValidationAppException("Trạng thái chiến dịch không hợp lệ.");
        }

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.Status = status;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    /// <summary>
    /// Sinh mã dạng <c>CD-2026-007</c>: đọc được, gõ lại được, nhìn là biết của năm nào.
    ///
    /// Đánh số theo NĂM để con số khỏi dài mãi ra. Lấy mã lớn nhất của năm rồi +1 thay vì đếm số
    /// dòng: đếm dòng thì xoá một chiến dịch là số tiếp theo trùng với mã đã từng phát ra.
    ///
    /// Hai người tạo đúng cùng lúc thì có thể đụng mã — chỉ mục duy nhất sẽ chặn và người sau phải
    /// bấm lại. Chấp nhận được: chiến dịch mỗi năm chỉ vài cái, và đổi lấy việc không phải giữ một
    /// bảng cấp số chạy song song.
    /// </summary>
    private async Task<string> SinhMaAsync()
    {
        var nam = DateTimeOffset.UtcNow.Year;
        var dau = $"CD-{nam.ToString(CultureInfo.InvariantCulture)}-";

        var cua = await repo.ListAsync(c => c.Code.StartsWith(dau));
        var lonNhat = cua
            .Select(c => int.TryParse(c.Code[dau.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return dau + (lonNhat + 1).ToString("D3", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tra chiến dịch theo mã rồi chia số — đây là cửa mà form thu lead đi vào.
    /// <c>null</c> khi mã không tra được: lead vẫn được tạo, chỉ là không gắn chiến dịch nào.
    /// </summary>
    /// <summary>
    /// Tra CẤU HÌNH chia số theo mã. Tách riêng để tầng ngoài bọc cache được — xem
    /// <see cref="CauHinhChiaSoDto"/>.
    /// </summary>
    public async Task<CauHinhChiaSoDto?> TimCauHinhChiaSoAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var ma = code.Trim();
        var tim = await repo.ListAsync(c => c.Code == ma);

        return tim.Count == 0
            ? null
            : new CauHinhChiaSoDto(tim[0].Id, tim[0].AssignMode, NhomChiaSo.Parse(tim[0].AssigneesJson));
    }

    public async Task<ChiaSoKetQuaDto?> ChiaSoAsync(string? code)
    {
        var cauHinh = await TimCauHinhChiaSoAsync(code);
        return cauHinh is null ? null : await ChiaSoTheoCauHinhAsync(cauHinh);
    }

    /// <summary>
    /// Đếm và chọn người từ một cấu hình ĐÃ CÓ. Tách ra để tầng bọc cache lấy cấu hình qua cache
    /// rồi gọi thẳng vào đây, khỏi tra lại cơ sở dữ liệu.
    /// </summary>
    public async Task<ChiaSoKetQuaDto> ChiaSoTheoCauHinhAsync(CauHinhChiaSoDto cauHinh)
    {
        ArgumentNullException.ThrowIfNull(cauHinh);

        // Con đếm để xoay vòng = số lead chiến dịch ĐÃ có. Đếm ở SQL (có chỉ mục theo CampaignId),
        // KHÔNG nạp bảng và KHÔNG cache.
        //
        // Vì sao không cache con số này: nó tăng theo từng lead. Cache dù chỉ vài giây thì mọi lead
        // rơi vào cùng khoảng đó đều đọc ra CÙNG một con đếm, nên cùng chia dư ra CÙNG một người —
        // vòng chia đứng im mà nhìn bên ngoài vẫn thấy "đang xoay vòng". Đúng thứ tính năng này
        // sinh ra để tránh, và không ai phát hiện được cho tới khi xem lại số liệu cuối tháng.
        var daCo = await leadRepo.CountAsync(l => l.CampaignId == cauHinh.CampaignId);

        var nguoi = ChiaSoSale.Chon((LeadAssignMode)cauHinh.AssignMode, cauHinh.Assignees, daCo);
        return new ChiaSoKetQuaDto(cauHinh.CampaignId, nguoi);
    }

    private static LeadCampaignDto Map(LeadCampaign c, List<Lead> leads, IReadOnlyDictionary<Guid, string> userNames)
    {
        var total = leads.Count;
        var cared = leads.Count(l => l.Status != LeadStatus.New);
        var closed = leads.Count(l => l.Status == LeadStatus.Won);
        var progress = total == 0 ? 0m : Math.Round(cared * 100m / total, 2);
        var closeRate = total == 0 ? 0m : Math.Round(closed * 100m / total, 2);
        return new LeadCampaignDto(
            c.Id, c.Name, c.CreatedByUserId,
            c.CreatedByUserId is { } uid ? userNames.GetValueOrDefault(uid) : null,
            c.CreatedAt, c.Status, total, cared, closed, progress, closeRate,
            c.Code, c.AssignMode, NhomChiaSo.Parse(c.AssigneesJson));
    }
}
