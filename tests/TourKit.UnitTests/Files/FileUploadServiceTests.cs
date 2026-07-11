using System.Text;
using TourKit.Application.Common;
using TourKit.Application.Files;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Booking; // FakeRepository<T> generic dùng chung

namespace TourKit.UnitTests.Files;

public sealed class FileUploadServiceTests
{
    private sealed class FakeFileStorage : IFileStorage
    {
        private readonly Dictionary<string, byte[]> _store = [];
        public int SaveCount { get; private set; }

        public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            var key = $"key/{Guid.NewGuid():N}";
            _store[key] = ms.ToArray();
            SaveCount++;
            return key;
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
            => Task.FromResult<Stream?>(_store.TryGetValue(storageKey, out var bytes) ? new MemoryStream(bytes) : null);
    }

    [Fact]
    public async Task SaveAsync_stores_content_and_metadata()
    {
        var storage = new FakeFileStorage();
        var service = new FileUploadService(new FakeRepository<FileUpload>(), storage);
        var bytes = Encoding.UTF8.GetBytes("hello");

        var dto = await service.SaveAsync("a.txt", "text/plain", bytes.Length, new MemoryStream(bytes));

        Assert.Equal("a.txt", dto.FileName);
        Assert.Equal(5, dto.Size);
        Assert.Equal(1, storage.SaveCount);

        var page = await service.ListAsync(1, 20);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task SaveAsync_defaults_contenttype_and_rejects_empty_filename()
    {
        var service = new FileUploadService(new FakeRepository<FileUpload>(), new FakeFileStorage());

        var dto = await service.SaveAsync("x.bin", "", 3, new MemoryStream([1, 2, 3]));
        Assert.Equal("application/octet-stream", dto.ContentType);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.SaveAsync("", "text/plain", 0, new MemoryStream()));
    }

    [Fact]
    public async Task OpenAsync_returns_content_and_metadata()
    {
        var service = new FileUploadService(new FakeRepository<FileUpload>(), new FakeFileStorage());
        var bytes = Encoding.UTF8.GetBytes("world");
        var dto = await service.SaveAsync("b.txt", "text/plain", bytes.Length, new MemoryStream(bytes));

        var (meta, content) = await service.OpenAsync(dto.Id);
        using var reader = new StreamReader(content);
        Assert.Equal("world", await reader.ReadToEndAsync());
        Assert.Equal("b.txt", meta.FileName);
    }

    [Fact]
    public async Task OpenAsync_missing_throws_NotFound()
    {
        var service = new FileUploadService(new FakeRepository<FileUpload>(), new FakeFileStorage());
        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenAsync(Guid.NewGuid()));
    }
}
