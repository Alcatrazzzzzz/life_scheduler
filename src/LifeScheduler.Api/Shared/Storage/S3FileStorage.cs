using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace LifeScheduler.Api.Shared.Storage;

public sealed class S3FileStorage(IAmazonS3 client, IOptions<StorageOptions> options) : IFileStorage
{
    private static readonly TimeSpan UploadTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DownloadTtl = TimeSpan.FromMinutes(5);

    private string Bucket => options.Value.S3.Bucket;

    public Task<UploadTicket> CreateUploadTicketAsync(
        Guid attachmentId,
        string storageKey,
        string contentType,
        CancellationToken ct)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(UploadTtl);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType,
        };

        var ticket = new UploadTicket(
            client.GetPreSignedURL(request),
            "PUT",
            new Dictionary<string, string> { ["Content-Type"] = contentType },
            expiresAt);

        return Task.FromResult(ticket);
    }

    public Task<DownloadTicket> CreateDownloadTicketAsync(
        Guid attachmentId,
        string storageKey,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(DownloadTtl);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = AttachmentContentDisposition.ForAttachment(fileName),
                ContentType = contentType,
            },
        };

        var ticket = new DownloadTicket(client.GetPreSignedURL(request), expiresAt);
        return Task.FromResult(ticket);
    }

    public async Task<StoredObject?> StatAsync(string storageKey, CancellationToken ct)
    {
        try
        {
            var metadata = await client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = Bucket, Key = storageKey },
                ct);

            return new StoredObject(metadata.ContentLength, metadata.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        await client.DeleteObjectAsync(
            new DeleteObjectRequest { BucketName = Bucket, Key = storageKey },
            ct);
    }
}
