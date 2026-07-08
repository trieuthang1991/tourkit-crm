using TourKit.Infrastructure.Entities;

namespace TourKit.Api.Crm;

public sealed record CreateLeadRequest(
    string FullName, string? Phone, string? Email, string? Source, Guid? AssignedToUserId);

public sealed record UpdateLeadRequest(
    string FullName, string? Phone, string? Email, string? Source, LeadStatus Status, Guid? AssignedToUserId);

public sealed record LeadResponse(
    Guid Id, string FullName, string? Phone, string? Email, string? Source,
    LeadStatus Status, Guid? AssignedToUserId, Guid? ConvertedCustomerId);

public sealed record ConvertLeadResponse(Guid CustomerId);
