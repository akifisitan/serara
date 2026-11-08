using Serara.Files;
using Serara.Options;
using Serara.Search;
using Serara.Tui;

namespace Serara.Console;

public static class Registrations
{
    public static IServiceCollection AddSeraraConsole(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSeraraFiles();
        services.AddSeraraSearch();
        services.AddSeraraTui();

        services
            .AddOptions<FileSearchOptions>()
            .BindConfiguration($"SearchOptions:{nameof(FileSearchOptions)}");
        services
            .AddOptions<ZipFileSearchOptions>()
            .BindConfiguration($"SearchOptions:{nameof(ZipFileSearchOptions)}");
        services
            .AddOptions<SeraraConsoleAppOptions>()
            .Bind(configuration.GetSection(nameof(SeraraConsoleAppOptions)))
            .WithValidator<SeraraConsoleAppOptions, SeraraConsoleAppOptionsValidator>();
        services
            .AddOptions<SearchExecutionOptions>()
            .BindConfiguration($"SearchOptions:{nameof(SearchExecutionOptions)}");
        services
            .AddOptions<KeyBindingOptions>()
            .BindConfiguration("KeyBindings")
            .WithValidator<KeyBindingOptions, KeyBindingOptionsValidator>();

        services.AddSingleton<IKeyRegistrationMapping, KeyRegistrationMapping>();
        services.AddSingleton<KeyBindingSet>();
        services.AddSingleton<KeyRegistrations>();
        services.AddSingleton<IPromptAdapter, PromptAdapter>();
        services.AddScoped<IConsoleInput, ConsoleInput>();

        services.AddSingleton<MainView>();
        services.AddSingleton<SearchResultsView>();
        services.AddSingleton<SearchRunView>();
        services.AddSingleton<OpenLogFileView>();
        services.AddSingleton<FileExplorerView>();
        services.AddSingleton<HelpView>();
        services.AddSingleton<SearchResultsChannel>();
        services.AddSingleton(sp => new SeraraConsoleApplication(
            sp.GetRequiredService<MainView>(),
            sp.GetRequiredService<IViewManager>()
        ));

        services.AddScoped<SearchRunOrchestrator>();
        services.AddScoped<ISearchRun, SearchRun>();
        services.AddScoped<ViewContext>();
        services.AddScoped<IViewContext>(sp => sp.GetRequiredService<ViewContext>());

        return services;
    }
}
