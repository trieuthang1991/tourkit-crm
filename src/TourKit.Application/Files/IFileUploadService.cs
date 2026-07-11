using TourKit.Application.Common;
using TourKit.Application.Files.Dtos;

namespace TourKit.Application.Files;

public interface IFileUploadService
{
    Task<PagedResult<FileUploadDto>> ListAsync(int page, int size);
    Task<FileUploadDto> SaveAsync(string fileName, string contentType, long size, Stream content, CancellationToken ct = default);
    Task<(FileUploadDto Meta, Stream Content)> OpenAsync(Guid id, CancellationToken ct = default);
}
