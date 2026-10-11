using Serara.Console;
using Serara.Core.Metadata;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Module.WinServer.Console;

internal sealed class MetadataProvider : IConsoleInitializer
{
    private const int maxAttempts = 3;

    private readonly ILogger<MetadataProvider> _logger;
    private readonly WinApplicationMetadataProvider _metadataService;
    private readonly IMultiViewConsole _console;

    public MetadataProvider(
        ILogger<MetadataProvider> logger,
        WinApplicationMetadataProvider metadataService,
        IMultiViewConsole console
    )
    {
        _logger = logger;
        _metadataService = metadataService;
        _console = console;
    }

    public Task Initialize(CancellationToken cancellationToken) => GetMetadata(cancellationToken);

    public async Task<IReadOnlyList<WinApplicationMetadata>> GetMetadata(
        CancellationToken cancellationToken
    )
    {
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _console.WriteLine($"[{Colors.Log}]Retrieving metadata...[/]");
                try
                {
                    var metadata = await _metadataService
                        .GetMetadata(cancellationToken)
                        .ConfigureAwait(false);
                    _console.Clear();
                    return metadata;
                }
                catch (MetadataLoadException ex) when (ex.IsRetryable && attempt < maxAttempts)
                {
                    _console.WriteLine($"[{Colors.Error}]{ex.Message.EscapeMarkup()}[/]");
                    _logger.ZLogWarning(ex, $"Failed to retrieve metadata on attempt {attempt}");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _console.WriteLine($"[{Colors.Error}]{ex.Message.EscapeMarkup()}[/]");
            _logger.ZLogError(ex, $"Failed to retrieve metadata");
            throw;
        }
    }
}
