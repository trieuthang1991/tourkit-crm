using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Reports;

namespace TourKit.Api.Controllers;

/// <summary>Báo cáo (read-only) — công nợ phải thu/phải trả, dashboard, dòng tiền, doanh thu–lợi nhuận, hoa hồng.</summary>
[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet("order-debt")]
    [Authorize(Permissions.ReportDebtView)]
    public async Task<IActionResult> OrderDebt()
    {
        var rows = await service.GetOrderDebtAsync();
        return Ok(rows);
    }

    [HttpGet("provider-debt")]
    [Authorize(Permissions.ReportProviderDebtView)]
    public async Task<IActionResult> ProviderDebt()
    {
        var rows = await service.GetProviderDebtAsync();
        return Ok(rows);
    }

    [HttpGet("dashboard")]
    [Authorize(Permissions.ReportDashboardView)]
    public async Task<IActionResult> Dashboard()
    {
        var summary = await service.GetDashboardAsync();
        return Ok(summary);
    }

    [HttpGet("cash-flow")]
    [Authorize(Permissions.ReportCashFlowView)]
    public async Task<IActionResult> CashFlow()
    {
        var rows = await service.GetCashFlowAsync();
        return Ok(rows);
    }

    [HttpGet("turnover")]
    [Authorize(Permissions.ReportTurnoverView)]
    public async Task<IActionResult> Turnover()
    {
        var rows = await service.GetTurnoverAsync();
        return Ok(rows);
    }

    [HttpGet("commission-by-user")]
    [Authorize(Permissions.ReportCommissionView)]
    public async Task<IActionResult> CommissionByUser()
    {
        var rows = await service.GetCommissionByUserAsync();
        return Ok(rows);
    }
}
