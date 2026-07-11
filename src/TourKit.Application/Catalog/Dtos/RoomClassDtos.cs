namespace TourKit.Application.Catalog.Dtos;

public sealed record RoomClassDto(Guid Id, string Name, int SortOrder, int Status);
public sealed record CreateRoomClassDto(string Name, int SortOrder);
public sealed record UpdateRoomClassDto(string Name, int SortOrder);
