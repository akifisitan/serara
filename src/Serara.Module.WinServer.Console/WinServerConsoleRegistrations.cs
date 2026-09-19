using Serara.Console;
using Serara.Core.Authentication;
using Serara.Files;
using Serara.History;

namespace Serara.Module.WinServer.Console;

public static class WinServerConsoleRegistrations
{
    public static IServiceCollection AddWinServerConsole(this IServiceCollection services)
    {
        services.AddScoped<WinServerConsoleInput>();
        services.AddScoped<MetadataProvider>();

        services.AddScoped<WinServerSearchWorkflow>(sp => new WinServerSearchWorkflow(
            sp.GetRequiredService<RetrieveSearchOptionsStateMachine>(),
            CreateSearchHistory(sp),
            sp.GetRequiredService<ILogger<WinServerSearchWorkflow>>()
        ));
        services.AddScoped<ISearchWorkflow>(sp => sp.GetRequiredService<WinServerSearchWorkflow>());
        services.AddScoped<WinServerFileExplorerAction>();
        services.AddScoped<IInteractiveAction>(sp =>
            sp.GetRequiredService<WinServerFileExplorerAction>()
        );

        services.AddScoped<GetHistoryOrRunChoiceStep>(sp => new GetHistoryOrRunChoiceStep(
            sp.GetRequiredService<WinServerConsoleInput>(),
            CreateSearchHistory(sp),
            sp.GetRequiredService<ILogger<GetHistoryOrRunChoiceStep>>()
        ));
        services.AddScoped<SelectFromSearchHistoryStep>(sp => new SelectFromSearchHistoryStep(
            sp.GetRequiredService<WinServerConsoleInput>(),
            CreateSearchHistory(sp),
            sp.GetRequiredService<MetadataProvider>()
        ));
        services.AddScoped<GetApplicationStep>();
        services.AddScoped<GetServersToSearchStep>();
        services.AddScoped<GetServerSearchStrategyStep>();
        services.AddScoped<GetSearchTimesStep>();
        services.AddScoped<GetSearchPatternStep>();
        services.AddScoped<AcceptAndStartStep>();
        services.AddScoped<
            BuilderStateMachine<RetrieveSearchRunOptionsStateMachineRecord, WinServerSearchRequest>
        >();
        services.AddScoped<RetrieveSearchOptionsStateMachine>();

        services.AddScoped<GetApplicationForFileExplorerStep>();
        services.AddScoped<GetLiveOrArchiveChoiceForFileExplorerStep>();
        services.AddScoped<GetServerForFileExplorerStep>();
        services.AddScoped<SelectDirectoryForFileExplorerStep>();
        services.AddScoped<
            BuilderStateMachine<RetrieveFileExplorerOptionsStateMachineState, Unit>
        >();
        services.AddScoped<RetrieveFileExplorerOptionsStateMachine>();

        services.AddScoped<GetUsernameCredentialStep>();
        services.AddScoped<GetPasswordCredentialStep>();
        services.AddScoped<
            BuilderStateMachine<CredentialProviderStateMachineState, BasicAuthCredential>
        >();
        services.AddScoped<CredentialProviderStateMachine>();
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
