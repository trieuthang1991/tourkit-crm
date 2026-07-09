using TourKit.Shared.Enums;

namespace TourKit.Application.Crm.Dtos;

public sealed record LeadDto(
    Guid Id, string FullName, string? Phone, string? Email, string? Source,
    LeadStatus Status, Guid? AssignedToUserId, Guid? ConvertedCustomerId);

public sealed record CreateLeadDto(
    string FullName, string? Phone, string? Email, string? Source, Guid? AssignedToUserId);

public sealed record UpdateLeadDto(
    string FullName, string? Phone, string? Email, string? Source, LeadStatus Status, Guid? AssignedToUserId);

public sealed record ConvertLeadResultDto(Guid CustomerId);
