using FluentValidation;

namespace Serara.Files;

public static class Registrations
{
    public static IServiceCollection AddSeraraFiles(this IServiceCollection services)
    {
        services.TryAddSingleton<ZipFileSearcher>();
        services.TryAddSingleton<FileSearcher>();
        services.TryAddSingleton<FileSystemEnumerator>();
        services.TryAddSingleton<ZipFileEntryExtractor>();

        services
            .AddSingleton<IValidator<FileSearchOptions>, FileSearchOptionsValidator>()
            .AddSingleton<
                IValidateOptions<FileSearchOptions>,
                FluentValidationOptions<FileSearchOptions>
            >();

        services
            .AddSingleton<IValidator<ZipFileSearchOptions>, ZipFileSearchOptionsValidator>()
            .AddSingleton<
                IValidateOptions<ZipFileSearchOptions>,
                FluentValidationOptions<ZipFileSearchOptions>
            >();

        services.TryAddSingleton<ITimeProvider, TimeProvider>();

        return services;
    }
}
