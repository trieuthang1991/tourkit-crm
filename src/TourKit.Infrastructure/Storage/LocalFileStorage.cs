using TourKit.Application.Files;
using TourKit.Shared.Tenancy;

namespace TourKit.Infrastructure.Storage;

/// <summary>
/// Lưu tệp trên đĩa local (dev) — conventions §8. StorageKey = "{tenantId}/{guid}{ext}": server sinh,
/// cô lập theo tenant, an toàn path-traversal (không dùng path từ client). Prod thay bằng S3/Azure impl.
/// </summary>
public sealed class LocalFileStorage(ITenantContext tenant, string root) : IFileStorage
{
    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var storageKey = $"{tenant.TenantId}/{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(root, storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        return storageKey;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(root, storageKey);
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }
}
