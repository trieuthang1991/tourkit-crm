namespace TourKit.Application.Customers.Dtos;

public sealed record CustomerDto(
    Guid Id, string FullName, string? Phone, int CustomerType, string? Source, string? Tag, decimal TempBalance);

// Field mới có default → không phá vỡ call site cũ (chỉ FullName/Phone) và request body cũ.
public sealed record CreateCustomerDto(
    string FullName, string? Phone,
    int CustomerType = 0, string? Source = null, string? Tag = null, decimal TempBalance = 0);

public sealed record UpdateCustomerDto(
    string FullName, string? Phone,
    int CustomerType = 0, string? Source = null, string? Tag = null, decimal TempBalance = 0);
