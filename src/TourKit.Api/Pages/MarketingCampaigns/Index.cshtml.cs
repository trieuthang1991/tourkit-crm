using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;
using TourKit.Application.Marketing;
using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.MarketingCampaigns;

// Danh sách chiến dịch: DataTables SERVER-SIDE (không get-all) + giữ đủ thông tin bản cũ
// (web/src/features/marketing/MarketingPage.tsx — phần DANH SÁCH CHIẾN DỊCH): 4 KPI, lọc
// từ khoá + kênh + trạng thái (CampaignListFilter), cột ghép Chiến dịch (tên+kênh) / Nội dung
// (chủ đề+trích nội dung), nút Log (nhật ký gửi), Sửa, Xoá.
// CRUD offcanvas: CampaignDto scalar (Name/Channel enum/Subject/Body/Status), không FK/collection con.
// Create bỏ qua Status (mặc định nháp); Update dùng Status.
[Authorize(Policy = "marketing.view")]
public class IndexModel : TkListPageModel
{
    /// <summary>Trần địa chỉ nhận cho MỘT lần gửi — chặn cả việc bấm nhầm lẫn việc treo request.</summary>
    public const int MaxRecipients = 500;

    private readonly ICampaignService _svc;
    private readonly ICustomerService _customers;

    public IndexModel(ICampaignService svc, ICustomerService customers)
    {
        _svc = svc;
        _customers = customers;
    }

    public CampaignStatsDto Stats { get; private set; } = new(0, 0, 0, 0);

    public bool CanCreate => User.HasClaim("perm", "marketing.create");
    public bool CanSend => User.HasClaim("perm", "marketing.send");

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public string Name { get; set; } = "";
        public int Channel { get; set; } = (int)MarketingChannel.Email;
        public string? Subject { get; set; }
        public string Body { get; set; } = "";
        public int Status { get; set; }
    }

    public static string ChannelLabel(MarketingChannel c) => c switch
    {
        MarketingChannel.Email => "Email",
        MarketingChannel.Sms => "SMS",
        MarketingChannel.Zalo => "Zalo",
        _ => c.ToString(),
    };

    public static string ChannelColor(MarketingChannel c) => c switch
    {
        MarketingChannel.Email => "primary",
        MarketingChannel.Sms => "warning",
        MarketingChannel.Zalo => "info",
        _ => "secondary",
    };

    public async Task OnGetAsync() => Stats = await _svc.GetStatsAsync();

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang, mọi tiêu chí đẩy xuống service.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var q = Request.Query;
        int? channel = int.TryParse(q["channel"], out var ch) ? ch : null;
        int? status = int.TryParse(q["status"], out var st) ? st : null;

        var result = await _svc.ListAsync(dt.Page, dt.Size, new CampaignListFilter(dt.Keyword, channel, status));
        var stats = await _svc.GetStatsAsync();

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            name = x.Name,
            channel = (int)x.Channel,
            channelLabel = ChannelLabel(x.Channel),
            channelColor = ChannelColor(x.Channel),
            subject = x.Subject,
            subjectText = string.IsNullOrWhiteSpace(x.Subject) ? "—" : x.Subject,
            body = x.Body,
            bodyPreview = x.Body.Length > 80 ? string.Concat(x.Body.AsSpan(0, 80), "…") : x.Body,
            status = x.Status,
            statusLabel = x.Status == 1 ? "Đã gửi" : "Nháp",
            statusColor = x.Status == 1 ? "success" : "secondary",
        });

        return DtJson(dt.Draw, stats.Total, result.Total, data);
    }

    /// <summary>Nhật ký gửi của 1 chiến dịch (drawer "Log" của bản cũ).</summary>
    public async Task<IActionResult> OnGetLogsAsync(Guid campaignId)
    {
        var logs = await _svc.ListLogsAsync(campaignId);
        return new JsonResult(logs.Select(l => new
        {
            id = l.Id,
            recipient = l.Recipient,
            statusLabel = l.Status == 1 ? "Thành công" : "Lỗi",
            statusColor = l.Status == 1 ? "success" : "danger",
            sentAtText = l.SentAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
        }));
    }

    /// <summary>
    /// Gợi ý địa chỉ nhận từ danh sách khách hàng — chỉ lấy tối đa MaxRecipients bản ghi CÓ liên hệ
    /// hợp lệ theo kênh (Email cần email, SMS/Zalo cần điện thoại). Trả cả danh sách để người gửi
    /// NHÌN THẤY đích trước khi bấm gửi, không gửi mù theo bộ lọc.
    /// </summary>
    public async Task<IActionResult> OnGetRecipientsAsync(int channel, string? q)
    {
        if (!CanSend)
        {
            return new JsonResult(Result.Error("Bạn không có quyền gửi chiến dịch."));
        }

        var wantEmail = (MarketingChannel)channel == MarketingChannel.Email;

        // Quét theo TRANG cho tới khi đủ trần — không get-all bảng khách hàng.
        const int pageSize = 200;
        var picked = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var page = 1;
        int total;

        do
        {
            var result = await _customers.ListAsync(page, pageSize, new CustomerListFilter(Q: q));
            total = result.Total;

            foreach (var c in result.Items)
            {
                var contact = (wantEmail ? c.Email : c.Phone)?.Trim();
                if (string.IsNullOrWhiteSpace(contact) || !seen.Add(contact))
                {
                    continue;
                }

                picked.Add(new { name = c.FullName, contact });
                if (picked.Count >= MaxRecipients)
                {
                    break;
                }
            }

            page++;
        }
        while (picked.Count < MaxRecipients && (page - 1) * pageSize < total);

        return new JsonResult(Result.Success("", new
        {
            items = picked,
            max = MaxRecipients,
            channelLabel = wantEmail ? "email" : "số điện thoại",
        }));
    }

    /// <summary>Gửi chiến dịch tới đúng danh sách người dùng đã xác nhận trên màn hình.</summary>
    public async Task<IActionResult> OnPostSendAsync(Guid id, string? recipients)
    {
        if (!CanSend)
        {
            return new JsonResult(Result.Error("Bạn không có quyền gửi chiến dịch."));
        }

        var list = (recipients ?? "")
            .Split(['\n', '\r', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (list.Length == 0)
        {
            return new JsonResult(Result.Error("Chưa có địa chỉ nhận nào."));
        }

        if (list.Length > MaxRecipients)
        {
            return new JsonResult(Result.Error($"Một lần chỉ gửi tối đa {MaxRecipients} địa chỉ (đang có {list.Length}). Hãy chia nhỏ danh sách."));
        }

        try
        {
            var result = await _svc.SendAsync(id, new SendCampaignDto(list));
            return new JsonResult(Result.Success($"Đã gửi {result.Sent} tin. Xem tab Nhật ký để biết địa chỉ nào lỗi.", new { sent = result.Sent }));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!CanCreate)
        {
            return new JsonResult(Result.Error("Bạn không có quyền sửa chiến dịch."));
        }

        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        var channel = (MarketingChannel)Input.Channel;
        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateCampaignDto(Input.Name, channel, Input.Subject, Input.Body, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateCampaignDto(Input.Name, channel, Input.Subject, Input.Body));
        }

        return new JsonResult(Result.Success("Đã lưu chiến dịch."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!CanCreate)
        {
            TempData["err"] = "Bạn không có quyền xoá chiến dịch.";
            return RedirectToPage();
        }

        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá chiến dịch.";
        return RedirectToPage();
    }
}
