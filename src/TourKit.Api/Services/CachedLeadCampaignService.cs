using TourKit.Application.Common;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Caching;
using TourKit.Infrastructure.Tenancy;

namespace TourKit.Api.Services;

/// <summary>
/// Bọc CACHE quanh phần tra cấu hình chia số của <see cref="LeadCampaignService"/>.
///
/// Vì sao cần: <c>ChiaSoAsync</c> chạy mỗi lần có một lead về từ form thu lead — tức có thể liên
/// tục trong một đợt chạy quảng cáo. Phần tra chiến dịch theo mã thì gần như không đổi (mã, chế độ,
/// nhóm người), nên gọi lại cơ sở dữ liệu cho từng lead là lãng phí thuần tuý.
///
/// CHỈ cache phần cấu hình. Con đếm để xoay vòng NẰM NGOÀI cache và vẫn đếm ở cơ sở dữ liệu mỗi
/// lần — xem chú thích trong <c>LeadCampaignService.ChiaSoAsync</c>: cache con số đó dù chỉ vài
/// giây là mọi lead trong khoảng đó cùng rơi vào một người, vòng chia đứng im mà bên ngoài vẫn
/// trông như đang xoay.
///
/// Là DECORATOR ở tầng Api chứ không nhét cache vào service nghiệp vụ: <c>TourKit.Application</c>
/// chỉ tham chiếu <c>TourKit.Shared</c>, không biết gì về Redis — và nên giữ nguyên như vậy.
///
/// Khoá cache tách theo tenant để không rò cấu hình giữa các đơn vị. Mọi đường GHI đều xoá cache
/// của chiến dịch đó ngay, nên sửa nhóm chia số có hiệu lực tức thì chứ không phải chờ hết hạn.
/// </summary>
public sealed class CachedLeadCampaignService(
    LeadCampaignService inner,
    ITkCache cache,
    AmbientTenantContext tenant) : ILeadCampaignService
{
    /// <summary>
    /// Ngắn thôi. Cấu hình ít đổi, nhưng khi đổi thì người dùng muốn thấy ngay; phần xoá-cache
    /// khi ghi đã lo phần lớn, thời hạn này chỉ là lưới đỡ cho những đường ghi chưa đi qua đây
    /// (ví dụ sửa thẳng dưới cơ sở dữ liệu).
    /// </summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private string Key(string code) => $"chiendich:{tenant.TenantId}:{code.Trim()}";

    public Task<CauHinhChiaSoDto?> TimCauHinhChiaSoAsync(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? Task.FromResult<CauHinhChiaSoDto?>(null)
            : cache.GetOrSetAsync(Key(code), Ttl, () => inner.TimCauHinhChiaSoAsync(code));

    /// <summary>
    /// Lấy cấu hình QUA CACHE rồi mới đếm và chọn — nên phải dựng lại ở đây thay vì gọi thẳng
    /// <c>inner.ChiaSoAsync</c>, vì lời gọi đó sẽ tự tra cơ sở dữ liệu và bỏ qua cache.
    /// </summary>
    public async Task<ChiaSoKetQuaDto?> ChiaSoAsync(string? code)
    {
        var cauHinh = await TimCauHinhChiaSoAsync(code);
        return cauHinh is null ? null : await inner.ChiaSoTheoCauHinhAsync(cauHinh);
    }

    // ---- Đường GHI: làm việc thật rồi XOÁ cache của chiến dịch vừa đụng ----

    public async Task<LeadCampaignDto> CreateAsync(CreateLeadCampaignDto dto)
    {
        var kq = await inner.CreateAsync(dto);
        await cache.RemoveAsync(Key(kq.Code));
        return kq;
    }

    public async Task UpdateAsync(Guid id, UpdateLeadCampaignDto dto)
    {
        // Lấy mã TRƯỚC khi ghi: mã không đổi được nên lấy lúc nào cũng thế, nhưng đọc trước thì
        // vẫn xoá đúng khoá kể cả sau này có ai cho phép đổi mã.
        var truoc = await inner.GetAsync(id);
        await inner.UpdateAsync(id, dto);
        await cache.RemoveAsync(Key(truoc.Code));
    }

    public async Task SetStatusAsync(Guid id, int status)
    {
        var truoc = await inner.GetAsync(id);
        await inner.SetStatusAsync(id, status);
        await cache.RemoveAsync(Key(truoc.Code));
    }

    // ---- Đường ĐỌC khác: không cache, đi thẳng ----
    // Danh sách và thẻ thống kê đọc số liệu lead đang đổi liên tục; cache vào là hiện số cũ ngay
    // trên màn người dùng vừa thao tác.

    public Task<PagedResult<LeadCampaignDto>> ListAsync(int page, int size, LeadCampaignListFilter? filter = null) =>
        inner.ListAsync(page, size, filter);

    public Task<LeadCampaignStatsDto> GetStatsAsync() => inner.GetStatsAsync();

    public Task<LeadCampaignDto> GetAsync(Guid id) => inner.GetAsync(id);
}
