using Amazon;
using Amazon.S3;

namespace LifeScheduler.Api.Shared.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(StorageOptions.SectionName);
        services.Configure<StorageOptions>(section);

        var options = section.Get<StorageOptions>() ?? new StorageOptions();

        if (options.UsesLocal)
        {
            services.AddScoped<LocalFileStorage>();
            services.AddScoped<IFileStorage>(provider => provider.GetRequiredService<LocalFileStorage>());
        }
        else
        {
            services.AddSingleton<IAmazonS3>(_ => CreateClient(options));
            services.AddScoped<IFileStorage, S3FileStorage>();
        }

        return services;
    }

    public static bool UsesLocalStorage(this IConfiguration configuration)
    {
        var provider = configuration[$"{StorageOptions.SectionName}:Provider"];
        return string.IsNullOrWhiteSpace(provider) ||
               string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase);
    }

    private static AmazonS3Client CreateClient(StorageOptions options)
    {
        var config = new AmazonS3Config { ForcePathStyle = options.S3.ForcePathStyle };

        if (!string.IsNullOrWhiteSpace(options.S3.ServiceUrl))
        {
            // S3-совместимые хранилища (MinIO и т.п.) требуют явного region для подписи.
            config.ServiceURL = options.S3.ServiceUrl;
            config.AuthenticationRegion = options.S3.Region;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.S3.Region);
        }

        // Креды берутся из стандартной цепочки AWS: env, профиль, IMDS.
        return new AmazonS3Client(config);
    }
}
