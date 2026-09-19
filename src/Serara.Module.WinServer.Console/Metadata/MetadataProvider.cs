using Serara.Tui;
using Spectre.Console;

namespace Serara.Module.WinServer.Console;

internal sealed class MetadataProvider
{
    private readonly ILogger<MetadataProvider> _logger;
    private readonly WinApplicationMetadataProvider _metadataService;
    private readonly MultiViewConsole _console;

    public MetadataProvider(
        ILogger<MetadataProvider> logger,
        WinApplicationMetadataProvider metadataService,
        MultiViewConsole console
    )
    {
        _logger = logger;
        _metadataService = metadataService;
        _console = console;
    }

    public async Task<IReadOnlyList<WinApplicationMetadata>> GetMetadata(
        CancellationToken cancellationToken
    )
    {
        try
        {
            _console.WriteLine($"[{Colors.Log}]Retrieving metadata...[/]");
            var metadata = await _metadataService
                .GetMetadata(cancellationToken)
                .ConfigureAwait(false);
            _console.Clear();
            return metadata;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _console.WriteLine(
                $"[{Colors.Error}]Failed to retrieve metadata: {ex.Message.EscapeMarkup()}[/]"
            );
            _logger.ZLogError(ex, $"Failed to retrieve metadata");
            throw;
        }
    }
}
