using Serara.Console;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Module.WinServer.Console;

internal sealed class SearchOptionsWorkflow
{
    private readonly WinServerConsoleInput _input;
    private readonly IConsoleInput _consoleInput;
    private readonly IMultiViewConsole _console;
    private readonly MetadataProvider _metadataProvider;
    private readonly WinServerSearchRequestPreparation _requestPreparation;
    private readonly SearchHistory? _history;
    private readonly ILogger<SearchOptionsWorkflow> _logger;

    public SearchOptionsWorkflow(
        WinServerConsoleInput input,
        IConsoleInput consoleInput,
        IMultiViewConsole console,
        MetadataProvider metadataProvider,
        WinServerSearchRequestPreparation requestPreparation,
        SearchHistory? history,
        ILogger<SearchOptionsWorkflow> logger
    )
    {
        _input = input;
        _consoleInput = consoleInput;
        _console = console;
        _metadataProvider = metadataProvider;
        _requestPreparation = requestPreparation;
        _history = history;
        _logger = logger;
    }

    public async Task<WinServerSearchRequest> Run(CancellationToken cancellationToken)
    {
        var draft = new SearchDraft();
        var screen = SearchScreen.ChooseSource;

        while (true)
        {
            switch (screen)
            {
                case SearchScreen.ChooseSource:
                {
                    var choice = await ChooseSource(draft, cancellationToken).ConfigureAwait(false);
                    screen = choice switch
                    {
                        SearchOptionChoice.FromHistory => SearchScreen.History,
                        SearchOptionChoice.NewRun => SearchScreen.Application,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case SearchScreen.History:
                {
                    var selection = await SelectHistory(draft, cancellationToken)
                        .ConfigureAwait(false);
                    screen = selection switch
                    {
                        HistorySelection.Complete => SearchScreen.Confirmation,
                        HistorySelection.MissingApplication => SearchScreen.Application,
                        HistorySelection.MissingServers => SearchScreen.Servers,
                        HistorySelection.Back => SearchScreen.ChooseSource,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case SearchScreen.Application:
                {
                    var result = await GetApplication(draft, cancellationToken)
                        .ConfigureAwait(false);
                    screen = result switch
                    {
                        PromptResult.Success => SearchScreen.Servers,
                        PromptResult.Cancel => SearchScreen.ChooseSource,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case SearchScreen.Servers:
                {
                    var result = await GetServers(draft, cancellationToken).ConfigureAwait(false);
                    screen = result switch
                    {
                        PromptResult.Success => SearchScreen.SearchStrategy,
                        PromptResult.Cancel => SearchScreen.Application,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case SearchScreen.SearchStrategy:
                {
                    var result = await GetSearchStrategy(draft, cancellationToken)
                        .ConfigureAwait(false);
                    screen = result switch
                    {
                        PromptResult.Success => SearchScreen.TimeRange,
                        PromptResult.Cancel => SearchScreen.Servers,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case SearchScreen.TimeRange:
                {
                    var result = await GetTimeRange(draft, cancellationToken).ConfigureAwait(false);
                    screen = result switch
                    {
                        PromptResult.Success => SearchScreen.SearchPattern,
                        PromptResult.Cancel => SearchScreen.SearchStrategy,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case SearchScreen.SearchPattern:
                {
                    var result = await GetSearchPattern(draft, cancellationToken)
                        .ConfigureAwait(false);
                    screen = result switch
                    {
                        PromptResult.Success => SearchScreen.Confirmation,
                        PromptResult.Cancel => SearchScreen.TimeRange,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case SearchScreen.Confirmation:
                {
                    var confirmation = await Confirm(draft, cancellationToken)
                        .ConfigureAwait(false);
                    if (confirmation is SearchAccepted accepted)
                    {
                        return accepted.Request;
                    }

                    screen = confirmation switch
                    {
                        SearchNeedsInput needsInput => needsInput.Screen,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                default:
                    throw new InvalidOperationException($"Unknown search screen: {screen}");
            }
        }
    }

    private async Task<SearchOptionChoice> ChooseSource(
        SearchDraft draft,
        CancellationToken cancellationToken
    )
    {
        draft.HistoryEntries = [];
        if (_history is null)
        {
            draft.IsFromHistory = false;
            return SearchOptionChoice.NewRun;
        }

        try
        {
            draft.HistoryEntries = await _history
                .RetrieveAll(cancellationToken)
                .ConfigureAwait(false);
            if (draft.HistoryEntries.Count == 0)
            {
                draft.IsFromHistory = false;
                return SearchOptionChoice.NewRun;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"Failed to initialize search history");
            draft.IsFromHistory = false;
            return SearchOptionChoice.NewRun;
        }

        var (result, choice) = await _input
            .GetHistoryOrRunChoice(cancellationToken)
            .ConfigureAwait(false);
        if (result != PromptResult.Success)
        {
            throw new InvalidOperationException();
        }

        draft.IsFromHistory = choice == SearchOptionChoice.FromHistory;
        return choice;
    }

    private async Task<HistorySelection> SelectHistory(
        SearchDraft draft,
        CancellationToken cancellationToken
    )
    {
        if (draft.HistoryEntries.Count == 0)
        {
            return HistorySelection.Back;
        }

        var (result, entry) = await _input
            .SelectFromSearchHistoryEntries(draft.HistoryEntries, cancellationToken)
            .ConfigureAwait(false);
        if (result == PromptResult.Cancel)
        {
            return HistorySelection.Back;
        }

        var payload = entry.Payload;
        draft.StartTime = payload.StartTime;
        draft.EndTime = payload.EndTime;
        draft.SearchPattern = payload.SearchPattern;
        draft.SearchStrategy = payload.ServerSearchStrategy;

        var metadata = await _metadataProvider.GetMetadata(cancellationToken).ConfigureAwait(false);
        var application = metadata.FirstOrDefault(application =>
            string.Equals(application.Name, payload.ApplicationName, StringComparison.Ordinal)
        );
        if (application is null)
        {
            return HistorySelection.MissingApplication;
        }

        draft.Application = application;
        var servers = new List<string>();
        foreach (var serverName in payload.SelectedServers)
        {
            var currentServerName = application.Servers.FirstOrDefault(server =>
                string.Equals(server, serverName, StringComparison.OrdinalIgnoreCase)
            );
            if (currentServerName is null)
            {
                return HistorySelection.MissingServers;
            }

            servers.Add(currentServerName);
        }

        draft.Servers = servers;
        if (result != PromptResult.Success)
        {
            throw new InvalidOperationException();
        }

        return HistorySelection.Complete;
    }

    private async Task<PromptResult> GetApplication(
        SearchDraft draft,
        CancellationToken cancellationToken
    )
    {
        var metadata = await _metadataProvider.GetMetadata(cancellationToken).ConfigureAwait(false);
        var (result, application) = await _input
            .GetApplication(metadata, cancellationToken)
            .ConfigureAwait(false);
        if (result == PromptResult.Cancel)
        {
            return result;
        }

        if (application.Servers.Count == 0)
        {
            _console.WriteLine(
                $"[{Colors.Error}]No servers found, please choose another application[/]"
            );
            return PromptResult.Cancel;
        }

        draft.Application = application;
        return result;
    }

    private async Task<PromptResult> GetServers(
        SearchDraft draft,
        CancellationToken cancellationToken
    )
    {
        var application =
            draft.Application
            ?? throw new InvalidOperationException(
                "Select an application before selecting servers."
            );
        var (result, servers) = await _input
            .GetServersToSearch(application, cancellationToken)
            .ConfigureAwait(false);
        if (result != PromptResult.Cancel)
        {
            draft.Servers = servers;
        }

        return result;
    }

    private async Task<PromptResult> GetSearchStrategy(
        SearchDraft draft,
        CancellationToken cancellationToken
    )
    {
        var application =
            draft.Application
            ?? throw new InvalidOperationException(
                "Select an application before selecting a search strategy."
            );
        var (result, strategy) = await _input
            .GetServerSearchStrategy(
                allowArchiveSearch: _requestPreparation.CanSearchArchive(application),
                cancellationToken
            )
            .ConfigureAwait(false);
        if (result != PromptResult.Cancel)
        {
            draft.SearchStrategy = strategy;
        }

        return result;
    }

    private async Task<PromptResult> GetTimeRange(
        SearchDraft draft,
        CancellationToken cancellationToken
    )
    {
        var (result, times) = await _input.GetSearchTimes(cancellationToken).ConfigureAwait(false);
        _console.Clear();
        if (result != PromptResult.Cancel)
        {
            draft.StartTime = times.StartTime;
            draft.EndTime = times.EndTime;
        }

        return result;
    }

    private async Task<PromptResult> GetSearchPattern(
        SearchDraft draft,
        CancellationToken cancellationToken
    )
    {
        var (result, pattern) = await _input
            .GetSearchPattern(cancellationToken)
            .ConfigureAwait(false);
        _console.Clear();
        if (result != PromptResult.Cancel)
        {
            draft.SearchPattern = pattern;
        }

        return result;
    }

    private async Task<SearchConfirmation> Confirm(
        SearchDraft draft,
        CancellationToken cancellationToken
    )
    {
        var preparation = _requestPreparation.Prepare(
            draft.Application,
            draft.Servers,
            draft.StartTime,
            draft.EndTime,
            draft.SearchPattern,
            draft.SearchStrategy
        );
        if (!preparation.IsValid)
        {
            foreach (var error in preparation.Errors)
            {
                _console.WriteLine($"[{Colors.Error}]{error.Message.EscapeMarkup()}[/]");
            }

            var screen = preparation.Errors[0].Field switch
            {
                WinServerSearchRequestField.Application => SearchScreen.Application,
                WinServerSearchRequestField.Servers => SearchScreen.Servers,
                WinServerSearchRequestField.SearchStrategy => SearchScreen.SearchStrategy,
                WinServerSearchRequestField.TimeRange => SearchScreen.TimeRange,
                WinServerSearchRequestField.SearchPattern => SearchScreen.SearchPattern,
                _ => throw new NotSupportedException(),
            };
            return new SearchNeedsInput(screen);
        }

        var request = preparation.Request!;
        _console.WriteLine(
            $"""
            [{Colors.Title}]Running query with the following parameters:[/]
            {request.ToConsoleDisplay()}
            """
        );

        var (result, accepted) = await _consoleInput
            .GetUserConfirmation(
                "Confirm and start search?",
                withCancel: true,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        _console.Clear();

        if (result == PromptResult.Cancel || !accepted)
        {
            return new SearchNeedsInput(
                draft.IsFromHistory ? SearchScreen.Application : SearchScreen.SearchPattern
            );
        }

        return new SearchAccepted(request);
    }

    private enum SearchScreen
    {
        ChooseSource,
        History,
        Application,
        Servers,
        SearchStrategy,
        TimeRange,
        SearchPattern,
        Confirmation,
    }

    private enum HistorySelection
    {
        Back,
        Complete,
        MissingApplication,
        MissingServers,
    }

    private abstract record SearchConfirmation;

    private sealed record SearchAccepted(WinServerSearchRequest Request) : SearchConfirmation;

    private sealed record SearchNeedsInput(SearchScreen Screen) : SearchConfirmation;

    private sealed class SearchDraft
    {
        public IReadOnlyList<WinServerSearchHistoryEntry> HistoryEntries { get; set; } = [];
        public bool IsFromHistory { get; set; }
        public WinApplicationMetadata? Application { get; set; }
        public IReadOnlyCollection<string>? Servers { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string? SearchPattern { get; set; }
        public WinServerSearchStrategy SearchStrategy { get; set; }
    }
}
