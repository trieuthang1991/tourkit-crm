namespace TourKit.Application.Customers.Dtos;

public sealed record CustomerDto(
    Guid Id, string? Code, string FullName, string? Phone, int CustomerType, string? Source, string? Tag, decimal TempBalance,
    string? Email, string? Address, DateTimeOffset? DateOfBirth,
    string? IdCardNumber, string? PassportNumber, DateTimeOffset? PassportExpiry, string? Nationality,
    // CRM bám hệ cũ (từ CrmProfileJson)
    string? Gender, string? City, string? MarketGroup, string? InitialNeed, string? CollaboratorName, string? Campaign,
    string? CreatedBy, string? CreatedByName,
    IReadOnlyList<string> Segments, IReadOnlyList<string> Tags,
    IReadOnlyList<string> AssignedTo, IReadOnlyList<string> AssignedToNames,
    DateTimeOffset CreatedAt,
    // Aggregate (chỉ ở danh sách)
    int PurchaseCount = 0, decimal Revenue = 0, DateTimeOffset? LastCareAt = null, string? LastCareContent = null);

// Field mới có default → không phá vỡ call site cũ. Code + CreatedBy KHÔNG nhận từ client.
public sealed record CreateCustomerDto(
    string FullName, string? Phone,
    int CustomerType = 0, string? Source = null, string? Tag = null, decimal TempBalance = 0,
    string? Email = null, string? Address = null, DateTimeOffset? DateOfBirth = null,
    string? IdCardNumber = null, string? PassportNumber = null, DateTimeOffset? PassportExpiry = null, string? Nationality = null,
    string? Gender = null, string? City = null, string? MarketGroup = null, string? InitialNeed = null,
    string? CollaboratorName = null, string? Campaign = null,
    IReadOnlyList<string>? Segments = null, IReadOnlyList<string>? Tags = null, IReadOnlyList<string>? AssignedTo = null);

public sealed record UpdateCustomerDto(
    string FullName, string? Phone,
    int CustomerType = 0, string? Source = null, string? Tag = null, decimal TempBalance = 0,
    string? Email = null, string? Address = null, DateTimeOffset? DateOfBirth = null,
    string? IdCardNumber = null, string? PassportNumber = null, DateTimeOffset? PassportExpiry = null, string? Nationality = null,
    string? Gender = null, string? City = null, string? MarketGroup = null, string? InitialNeed = null,
    string? CollaboratorName = null, string? Campaign = null,
    IReadOnlyList<string>? Segments = null, IReadOnlyList<string>? Tags = null, IReadOnlyList<string>? AssignedTo = null);
