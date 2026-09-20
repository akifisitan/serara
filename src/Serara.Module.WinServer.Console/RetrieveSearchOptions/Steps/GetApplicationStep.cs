using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetApplicationStep
    : BuilderStateMachineStep<RetrieveSearchRunOptionsStateMachineRecord, Unit>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly IMultiViewConsole _console;
    private readonly MetadataProvider _metadataProvider;

    public GetApplicationStep(
        WinServerConsoleInput consoleInput,
        IMultiViewConsole console,
        MetadataProvider metadataProvider
    )
    {
        _consoleInput = consoleInput;
        _console = console;
        _metadataProvider = metadataProvider;
    }

    protected override async Task<PromptResultRecord<Unit>> ExecuteAsync(
        RetrieveSearchRunOptionsStateMachineRecord input,
        CancellationToken cancellationToken
    )
    {
        var winApplicationMetadataList = await _metadataProvider
            .GetMetadata(cancellationToken)
            .ConfigureAwait(false);

        var (result, value) = await _consoleInput
            .GetApplication(winApplicationMetadataList, cancellationToken)
            .ConfigureAwait(false);

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        if (value.Servers.Count == 0)
        {
            _console.WriteLine(
                $"[{Colors.Error}]No servers found, please choose another application[/]"
            );
            return PromptResult.Cancel;
        }

        input.SelectedData = value;

        return result;
    }
}
