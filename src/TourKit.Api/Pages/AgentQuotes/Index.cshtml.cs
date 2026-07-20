using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.AgentQuotes;

// List-only (read-only): create/lifecycle phức tạp — CreateAsync cần AgentId (đại lý gửi qua portal)
// rồi QuoteAsync/ConfirmAsync/RejectAsync; KHÔNG có Delete → chỉ hiển thị danh sách.
[Authorize(Policy = "agentquote.view")]
public class IndexModel : PageModel
{
    private readonly IAgentQuoteRequestService _svc;
    public IndexModel(IAgentQuoteRequestService svc) => _svc = svc;

    public IReadOnlyList<AgentQuoteRequestDto> Items { get; private set; } = [];

    public static string StatusLabel(AgentQuoteStatus s) => s switch
    {
        AgentQuoteStatus.Requested => "Gửi yêu cầu",
        AgentQuoteStatus.Quoted => "Đã chào giá",
        AgentQuoteStatus.Confirmed => "Đã xác nhận",
        AgentQuoteStatus.Rejected => "Từ chối",
        _ => s.ToString(),
    };

    public static string StatusColor(AgentQuoteStatus s) => s switch
    {
        AgentQuoteStatus.Quoted => "info",
        AgentQuoteStatus.Confirmed => "success",
        AgentQuoteStatus.Rejected => "danger",
        _ => "warning",
    };

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;
}
