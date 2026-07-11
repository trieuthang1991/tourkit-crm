namespace TourKit.Application.Sales.Dtos;

public sealed record InvoiceLineDto(
    Guid Id, string Description, int Quantity, decimal UnitPrice, decimal VatRate, decimal LineAmount, decimal LineVat);

public sealed record InvoiceDto(
    Guid Id, string Series, string Number, DateTimeOffset InvoiceDate, Guid? OrderId,
    string BuyerName, string? BuyerTaxCode, string? BuyerAddress,
    decimal Subtotal, decimal VatAmount, decimal TotalAmount, int Status, string? Note, InvoiceLineDto[] Lines);

public sealed record InvoiceSummaryDto(
    Guid Id, string Series, string Number, DateTimeOffset InvoiceDate, string BuyerName, decimal TotalAmount, int Status);

public sealed record CreateInvoiceLineDto(string Description, int Quantity, decimal UnitPrice, decimal VatRate);

public sealed record CreateInvoiceDto(
    string Series, string Number, DateTimeOffset InvoiceDate, Guid? OrderId,
    string BuyerName, string? BuyerTaxCode, string? BuyerAddress, int Status, string? Note,
    CreateInvoiceLineDto[] Lines);

public sealed record UpdateInvoiceDto(
    string Series, string Number, DateTimeOffset InvoiceDate, Guid? OrderId,
    string BuyerName, string? BuyerTaxCode, string? BuyerAddress, int Status, string? Note,
    CreateInvoiceLineDto[] Lines);
