namespace TourKit.Application.Customers;

public sealed record CustomerDto(Guid Id, string FullName, string? Phone);
public sealed record CreateCustomerDto(string FullName, string? Phone);
public sealed record UpdateCustomerDto(string FullName, string? Phone);
