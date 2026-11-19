namespace LifeScheduler.Api.Shared.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";
    public LocalStorageOptions Local { get; set; } = new();
    public S3StorageOptions S3 { get; set; } = new();

    public bool UsesLocal =>
        string.IsNullOrWhiteSpace(Provider) ||
        string.Equals(Provider, "Local", StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalStorageOptions
{
    public string RootPath { get; set; } = "App_Data/attachments";
}

public sealed class S3StorageOptions
{
    public string Bucket { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
    public string? ServiceUrl { get; set; }
    public bool ForcePathStyle { get; set; }
}
