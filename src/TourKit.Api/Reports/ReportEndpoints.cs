using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Reports.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Reports;

/// <summary>
/// Báo cáo công nợ phải thu (legacy MoneyReport CNPT). Endpoint mỏng: dispatch query → map Result sang HTTP.
/// </summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/reports/order-debt", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new OrderDebtReportQuery(), ct))
                .Match(rows => Results.Ok(rows))).RequireAuthorization(Permissions.ReportDebtView);

        app.MapGet("/api/v1/reports/provider-debt", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ProviderDebtReportQuery(), ct))
                .Match(rows => Results.Ok(rows))).RequireAuthorization(Permissions.ReportProviderDebtView);

        app.MapGet("/api/v1/reports/dashboard", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DashboardReportQuery(), ct))
                .Match(s => Results.Ok(s))).RequireAuthorization(Permissions.ReportDashboardView);

        app.MapGet("/api/v1/reports/cash-flow", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new CashFlowReportQuery(), ct))
                .Match(rows => Results.Ok(rows))).RequireAuthorization(Permissions.ReportCashFlowView);

        app.MapGet("/api/v1/reports/turnover", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new TurnoverReportQuery(), ct))
                .Match(rows => Results.Ok(rows))).RequireAuthorization(Permissions.ReportTurnoverView);

        app.MapGet("/api/v1/reports/commission-by-user", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new CommissionByUserReportQuery(), ct))
                .Match(rows => Results.Ok(rows))).RequireAuthorization(Permissions.ReportCommissionView);

        return app;
    }
}
