namespace TourKit.Api.Crm;

/// <summary>DTO tạo bản ghi chăm sóc khách hàng mới.</summary>
public sealed record CreateCustomerCareRequest(Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt, Guid? AssignedToUserId, int Status);

/// <summary>DTO cập nhật chăm sóc khách hàng.</summary>
public sealed record UpdateCustomerCareRequest(string Title, string? Detail, DateTimeOffset? RemindAt, string? Feedback, Guid? AssignedToUserId, int Status);

/// <summary>DTO trả ra cho client (không lộ entity).</summary>
public sealed record CustomerCareResponse(Guid Id, Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt, string? Feedback, Guid? AssignedToUserId, int Status);
