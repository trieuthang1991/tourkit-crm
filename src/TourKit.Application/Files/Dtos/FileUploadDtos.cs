namespace TourKit.Application.Files.Dtos;

public sealed record FileUploadDto(Guid Id, string FileName, string ContentType, long Size, DateTimeOffset CreatedAt);
