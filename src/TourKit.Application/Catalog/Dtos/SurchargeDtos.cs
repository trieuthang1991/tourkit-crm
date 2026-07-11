namespace TourKit.Application.Catalog.Dtos;

public sealed record SurchargeDto(Guid Id, string Name, int CalcType, decimal DefaultValue, int SortOrder, int Status);

public sealed record CreateSurchargeDto(string Name, int CalcType, decimal DefaultValue, int SortOrder);

public sealed record UpdateSurchargeDto(string Name, int CalcType, decimal DefaultValue, int SortOrder);
