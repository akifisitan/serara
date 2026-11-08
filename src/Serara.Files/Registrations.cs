using Serara.Options;

namespace Serara.Files;

public static class Registrations
{
    public static IServiceCollection AddSeraraFiles(this IServiceCollection services)
    {
        services.TryAddSingleton<ZipFileSearcher>();
        services.TryAddSingleton<ILogRotation, LogRotation>();
        services.TryAddSingleton<FileSearcher>();
        services.TryAddSingleton<FileSystemEnumerator>();
        services.TryAddSingleton<ZipFileEntryExtractor>();

        services
            .AddOptions<FileSearchOptions>()
            .WithValidator<FileSearchOptions, FileSearchOptionsValidator>();

        services
            .AddOptions<ZipFileSearchOptions>()
            .WithValidator<ZipFileSearchOptions, ZipFileSearchOptionsValidator>();

        services.TryAddSingleton<ITimeProvider, TimeProvider>();

        return services;
    }
}
