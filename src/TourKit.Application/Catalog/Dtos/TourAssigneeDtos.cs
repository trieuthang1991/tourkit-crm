using TourKit.Shared.Enums;

namespace TourKit.Application.Catalog.Dtos;

public sealed record AssigneeDto(Guid Id, Guid UserId, AssigneeRole Role);
