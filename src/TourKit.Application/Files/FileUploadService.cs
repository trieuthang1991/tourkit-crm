using TourKit.Application.Common;
using TourKit.Application.Files.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Files;

/// <summary>Đính kèm tệp — lưu metadata (DB, cô lập tenant) + nội dung (IFileStorage). Chỉ tạo/đọc/liệt kê.</summary>
public sealed class FileUploadService(IRepository<FileUpload> repo, IFileStorage storage) : IFileUploadService
{
    public async Task<PagedResult<FileUploadDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        return new PagedResult<FileUploadDto>(items.Select(Map).ToList(), total, page, size);
    }

    public async Task<FileUploadDto> SaveAsync(string fileName, string contentType, long size, Stream content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationAppException("Tên tệp bắt buộc.");
        }

        var storageKey = await storage.SaveAsync(fileName, content, ct);

        var entity = new FileUpload
        {
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Size = size,
            StorageKey = storageKey,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task<(FileUploadDto Meta, Stream Content)> OpenAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var content = await storage.OpenReadAsync(entity.StorageKey, ct) ?? throw new NotFoundException();
        return (Map(entity), content);
    }

    private static FileUploadDto Map(FileUpload f) => new(f.Id, f.FileName, f.ContentType, f.Size, f.CreatedAt);
}
