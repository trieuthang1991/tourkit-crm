namespace TourKit.Application.Catalog.Dtos;

public sealed record PaymentTermDto(Guid Id, string Name, string? Description, int SortOrder, int Status);

public sealed record CreatePaymentTermDto(string Name, string? Description, int SortOrder);

public sealed record UpdatePaymentTermDto(string Name, string? Description, int SortOrder);
