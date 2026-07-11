namespace TourKit.Application.Sales.Dtos;

public sealed record QuoteLineDto(Guid Id, string Description, int Quantity, decimal UnitPrice, decimal Amount);

public sealed record QuoteDto(
    Guid Id, string Code, Guid? CustomerId, string CustomerName, string Title,
    DateTimeOffset? ValidUntil, int Status, string? Note, decimal TotalAmount, QuoteLineDto[] Lines);

/// <summary>Dòng tóm tắt cho danh sách (không kèm chi tiết dòng).</summary>
public sealed record QuoteSummaryDto(
    Guid Id, string Code, string CustomerName, string Title, DateTimeOffset? ValidUntil, int Status, decimal TotalAmount);

public sealed record CreateQuoteLineDto(string Description, int Quantity, decimal UnitPrice);

public sealed record CreateQuoteDto(
    string Code, Guid? CustomerId, string CustomerName, string Title,
    DateTimeOffset? ValidUntil, int Status, string? Note, CreateQuoteLineDto[] Lines);

public sealed record UpdateQuoteDto(
    string Code, Guid? CustomerId, string CustomerName, string Title,
    DateTimeOffset? ValidUntil, int Status, string? Note, CreateQuoteLineDto[] Lines);
