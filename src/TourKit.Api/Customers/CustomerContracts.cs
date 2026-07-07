namespace TourKit.Api.Customers;

/// <summary>DTO tạo khách hàng mới.</summary>
public sealed record CreateCustomerRequest(string FullName, string? Phone);

/// <summary>DTO cập nhật khách hàng.</summary>
public sealed record UpdateCustomerRequest(string FullName, string? Phone);

/// <summary>DTO trả ra cho client (không lộ entity).</summary>
public sealed record CustomerResponse(Guid Id, string FullName, string? Phone);
