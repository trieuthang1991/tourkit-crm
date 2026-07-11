namespace TourKit.Application.B2B.Dtos;

public sealed record AgentDto(
    Guid Id, string Code, string Name, string? ContactPerson, string? Phone, string? Email,
    string? TaxCode, string? Address, decimal CreditLimit, int Status);

public sealed record CreateAgentDto(
    string Code, string Name, string? ContactPerson, string? Phone, string? Email,
    string? TaxCode, string? Address, decimal CreditLimit, int Status);

public sealed record UpdateAgentDto(
    string Code, string Name, string? ContactPerson, string? Phone, string? Email,
    string? TaxCode, string? Address, decimal CreditLimit, int Status);
