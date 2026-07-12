namespace TourKit.Application.Customers.Dtos;

/// <summary>Thẻ thống kê đầu màn Data khách hàng (bám hệ cũ).</summary>
public sealed record CustomerStatsDto(
    int Total, int NewToday, int NewThisMonth, int FirstTimeBuyers, int RepeatBuyers);

/// <summary>Giá trị có sẵn cho các dropdown lọc (facets) — gom distinct từ data thực để user chọn, không gõ tay.</summary>
public sealed record CustomerFilterOptionsDto(
    IReadOnlyList<string> Sources, IReadOnlyList<string> Cities, IReadOnlyList<string> MarketGroups,
    IReadOnlyList<string> Campaigns, IReadOnlyList<string> Collaborators, IReadOnlyList<string> Branches,
    IReadOnlyList<string> Groups, IReadOnlyList<string> Departments,
    IReadOnlyList<string> Tags, IReadOnlyList<string> Segments);

public sealed record CustomerDto(
    Guid Id, string? Code, string FullName, string? Phone, int CustomerType, string? Source, string? Tag, decimal TempBalance,
    string? Email, string? Address, DateTimeOffset? DateOfBirth,
    string? IdCardNumber, string? PassportNumber, DateTimeOffset? PassportExpiry, string? Nationality,
    // CRM bám hệ cũ (từ CrmProfileJson)
    string? Gender, string? City, string? MarketGroup, string? InitialNeed, string? CollaboratorName, string? Campaign,
    string? Branch, string? Group, string? Department,
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
    string? Branch = null, string? Group = null, string? Department = null,
    IReadOnlyList<string>? Segments = null, IReadOnlyList<string>? Tags = null, IReadOnlyList<string>? AssignedTo = null);

/// <summary>Bộ lọc màn Data khách hàng (bám thanh lọc mở rộng hệ cũ). Tất cả optional.</summary>
public sealed record CustomerListFilter(
    string? Q = null, int? CustomerType = null,
    string? Source = null, string? City = null, string? Gender = null, string? MarketGroup = null,
    string? Collaborator = null, string? Campaign = null, string? Branch = null, string? Group = null,
    string? Department = null, string? Segment = null, string? Tag = null, string? AssignedTo = null,
    string? CreatedBy = null,
    DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedTo = null,
    DateTimeOffset? CareFrom = null, DateTimeOffset? CareTo = null,
    decimal? RevenueFrom = null, decimal? RevenueTo = null,
    int? BirthdayMonth = null);

public sealed record UpdateCustomerDto(
    string FullName, string? Phone,
    int CustomerType = 0, string? Source = null, string? Tag = null, decimal TempBalance = 0,
    string? Email = null, string? Address = null, DateTimeOffset? DateOfBirth = null,
    string? IdCardNumber = null, string? PassportNumber = null, DateTimeOffset? PassportExpiry = null, string? Nationality = null,
    string? Gender = null, string? City = null, string? MarketGroup = null, string? InitialNeed = null,
    string? CollaboratorName = null, string? Campaign = null,
    string? Branch = null, string? Group = null, string? Department = null,
    IReadOnlyList<string>? Segments = null, IReadOnlyList<string>? Tags = null, IReadOnlyList<string>? AssignedTo = null);
