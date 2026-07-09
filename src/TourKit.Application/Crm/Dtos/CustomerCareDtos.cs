namespace TourKit.Application.Crm.Dtos;

public sealed record CustomerCareDto(
    Guid Id, Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt,
    string? Feedback, Guid? AssignedToUserId, int Status);

public sealed record CreateCustomerCareDto(
    Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt, Guid? AssignedToUserId, int Status);

public sealed record UpdateCustomerCareDto(
    string Title, string? Detail, DateTimeOffset? RemindAt, string? Feedback, Guid? AssignedToUserId, int Status);
