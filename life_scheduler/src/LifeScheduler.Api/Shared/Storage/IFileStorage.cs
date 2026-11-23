namespace LifeScheduler.Api.Shared.Storage;

public sealed record UploadTicket(
    string Url,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAt);

public sealed record DownloadTicket(string Url, DateTimeOffset ExpiresAt);

public sealed record StoredObject(long SizeBytes, string? ContentType);

public interface IFileStorage
{
    Task<UploadTicket> CreateUploadTicketAsync(
        Guid attachmentId,
        string storageKey,
        string contentType,
        CancellationToken ct);

    Task<DownloadTicket> CreateDownloadTicketAsync(
        Guid attachmentId,
        string storageKey,
        string fileName,
        string contentType,
        CancellationToken ct);

    Task<StoredObject?> StatAsync(string storageKey, CancellationToken ct);

    Task DeleteAsync(string storageKey, CancellationToken ct);
}
