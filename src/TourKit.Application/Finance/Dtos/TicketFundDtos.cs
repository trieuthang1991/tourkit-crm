namespace TourKit.Application.Finance.Dtos;

public sealed record TicketFundDto(
    Guid Id, Guid OrderId, Guid? ProviderId, Guid? ProviderServiceId, string TicketCode, int Status, bool IsClosed);

public sealed record CreateTicketFundDto(
    Guid OrderId, Guid? ProviderId, Guid? ProviderServiceId, string? TicketCode, int Status, bool IsClosed);

public sealed record UpdateTicketFundDto(
    Guid? ProviderId, Guid? ProviderServiceId, string? TicketCode, int Status, bool IsClosed);
