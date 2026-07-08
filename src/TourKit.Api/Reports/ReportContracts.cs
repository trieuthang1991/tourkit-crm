namespace TourKit.Api.Reports;

/// <summary>Một dòng công nợ đơn hàng: tổng phải thu, đã thu (phiếu đã duyệt), còn nợ.</summary>
public sealed record OrderDebtRow(
    Guid OrderId, string OrderCode, Guid CustomerId, decimal Total, decimal Paid, decimal Outstanding);
