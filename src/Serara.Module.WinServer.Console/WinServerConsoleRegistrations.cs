using Serara.Console;
using Serara.Core.Authentication;
using Serara.Files;
using Serara.History;
using Serara.Tui;

namespace Serara.Module.WinServer.Console;

public static class WinServerConsoleRegistrations
{
    public static IServiceCollection AddWinServerConsole(this IServiceCollection services)
    {
        services.AddScoped<WinServerConsoleInput>();
        services.AddScoped<MetadataProvider>();
        services.AddScoped<IConsoleInitializer>(sp => sp.GetRequiredService<MetadataProvider>());

        services.AddScoped<WinServerSearchWorkflow>(sp => new WinServerSearchWorkflow(
            sp.GetRequiredService<SearchOptionsWorkflow>(),
            CreateSearchHistory(sp),
            sp.GetRequiredService<ILogger<WinServerSearchWorkflow>>()
        ));
        services.AddScoped<ISearchWorkflow>(sp => sp.GetRequiredService<WinServerSearchWorkflow>());
        services.AddScoped<WinServerFileExplorerAction>();
        services.AddScoped<IInteractiveAction>(sp =>
            sp.GetRequiredService<WinServerFileExplorerAction>()
        );

        services.AddScoped<SearchOptionsWorkflow>(sp => new SearchOptionsWorkflow(
            sp.GetRequiredService<WinServerConsoleInput>(),
            sp.GetRequiredService<IConsoleInput>(),
            sp.GetRequiredService<IMultiViewConsole>(),
            sp.GetRequiredService<MetadataProvider>(),
            sp.GetRequiredService<WinServerSearchRequestPreparation>(),
            CreateSearchHistory(sp),
            sp.GetRequiredService<ILogger<SearchOptionsWorkflow>>()
        ));
        services.AddScoped<FileExplorerWorkflow>();
        services.AddScoped<CredentialWorkflow>();
        services.AddScoped<IAuthenticationHandler<BasicAuthCredential>, AuthenticationHandler>();

        return services;
    }

    private static SearchHistory? CreateSearchHistory(IServiceProvider serviceProvider)
    {
        var store = serviceProvider.GetService<ISearchHistoryStore>();
        return store is null
            ? null
            : new SearchHistory(store, serviceProvider.GetRequiredService<ITimeProvider>());
    }
}
