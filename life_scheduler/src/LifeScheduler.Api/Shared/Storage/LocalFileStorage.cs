using Microsoft.Extensions.Options;

namespace LifeScheduler.Api.Shared.Storage;

/// <summary>
/// Dev-хранилище: файлы лежат в App_Data, а тикеты ведут на эндпоинты самого API.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private static readonly TimeSpan UploadTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DownloadTtl = TimeSpan.FromMinutes(5);

    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options, IWebHostEnvironment environment)
    {
        var configured = options.Value.Local.RootPath;
        _root = Path.GetFullPath(
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(environment.ContentRootPath, configured));
    }

    public Task<UploadTicket> CreateUploadTicketAsync(
        Guid attachmentId,
        string storageKey,
        string contentType,
        CancellationToken ct)
    {
        var ticket = new UploadTicket(
            $"/api/storage/uploads/{attachmentId:N}",
            "PUT",
            new Dictionary<string, string> { ["Content-Type"] = contentType },
            DateTimeOffset.UtcNow.Add(UploadTtl));

        return Task.FromResult(ticket);
    }

    public Task<DownloadTicket> CreateDownloadTicketAsync(
        Guid attachmentId,
        string storageKey,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        var ticket = new DownloadTicket(
            $"/api/storage/download/{attachmentId:N}",
            DateTimeOffset.UtcNow.Add(DownloadTtl));

        return Task.FromResult(ticket);
    }

    public Task<StoredObject?> StatAsync(string storageKey, CancellationToken ct)
    {
        var path = ResolvePath(storageKey);
        var info = new FileInfo(path);

        return Task.FromResult<StoredObject?>(
            info.Exists ? new StoredObject(info.Length, null) : null);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    public async Task WriteAsync(string storageKey, Stream content, CancellationToken ct)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Пишем во временный файл и подменяем атомарно, чтобы не оставить полуфайл.
        var tempPath = path + ".uploading";
        await using (var file = File.Create(tempPath))
        {
            await content.CopyToAsync(file, ct);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    public Stream OpenRead(string storageKey)
    {
        var path = ResolvePath(storageKey);
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private string ResolvePath(string storageKey)
    {
        var relative = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_root, relative));

        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ключ объекта выходит за пределы хранилища.");

        return fullPath;
    }
}
