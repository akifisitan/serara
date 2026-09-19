using Serara.Files;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Module.WinServer.Console;

internal sealed class WinServerConsoleInput
{
    private readonly MultiViewConsole _console;
    private readonly IPromptAdapter _promptAdapter;
    private readonly WinServerSearchRequestPreparation _requestPreparation;

    private const string titleColor = Colors.DarkSeaGreen;

    public WinServerConsoleInput(
        MultiViewConsole console,
        IPromptAdapter promptAdapter,
        WinServerSearchRequestPreparation requestPreparation
    )
    {
        _console = console;
        _promptAdapter = promptAdapter;
        _requestPreparation = requestPreparation;
    }

    public Task<PromptResultRecord<string>> GetUsername(CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>($"[{titleColor}]Username:[/]").Validate(x =>
        {
            const string name = "Username";

            if (string.IsNullOrWhiteSpace(x))
            {
                return ValidationResult.Error($"{name} must not be empty");
            }

            return ValidationResult.Success();
        });

        return _console.Prompt(() => _promptAdapter.Setup(prompt), cancellationToken);
    }

    public Task<PromptResultRecord<string>> GetPassword(CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>($"[{titleColor}]Password:[/]")
            .Secret()
            .Validate(x =>
            {
                const string name = "Password";

                if (string.IsNullOrWhiteSpace(x))
                {
                    return ValidationResult.Error($"{name} must not be empty");
                }
                return ValidationResult.Success();
            });

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withCancel: true),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<WinApplicationMetadata>> GetApplication(
        IReadOnlyList<WinApplicationMetadata> choices,
        CancellationToken cancellationToken
    )
    {
        var prompt = new SelectionPrompt<WinApplicationMetadata>()
            .Title($"[{titleColor}]Select application to search[/]")
            .AddChoices(choices)
            .UseConverter(x => x.Name.EscapeMarkup())
            .SearchPlaceholderText($"[{Colors.Gray}](Type to filter): [/]")
            .WrapAround()
            .UseSearchFilter(
                (x, search) =>
                    x.SearchTerm.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.TeamName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Servers.Any(server =>
                        server.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
            );

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withNavigation: true, withCancel: true),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<WinApplicationMetadata>> GetApplicationForFileExplorer(
        IReadOnlyList<WinApplicationMetadata> choices,
        CancellationToken cancellationToken
    )
    {
        var prompt = new SelectionPrompt<WinApplicationMetadata>()
            .Title($"[{titleColor}]Select application to traverse[/]")
            .AddChoices(choices)
            .UseConverter(x => x.Name.EscapeMarkup())
            .SearchPlaceholderText($"[{Colors.Gray}](Type to filter): [/]")
            .WrapAround()
            .UseSearchFilter(
                (x, search) =>
                    x.SearchTerm.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.TeamName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Servers.Any(server =>
                        server.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
            );

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withNavigation: true),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<List<string>>> GetServersToSearch(
        WinApplicationMetadata choice,
        CancellationToken cancellationToken
    )
    {
        var prompt = new MultiSelectionPrompt<string>()
            .Title($"[{titleColor}]Select servers to search[/]")
            .SearchPlaceholderText($"[{Colors.Gray}](Type to filter): [/]")
            .AddChoices(choice.Servers)
            .UseConverter(x => x.EscapeMarkup())
            .WrapAround()
            .UseSearchFilter((x, search) => x.Contains(search, StringComparison.OrdinalIgnoreCase));

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withNavigation: true, withCancel: true),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<string>> GetServerForFileExplorer(
        WinApplicationMetadata choice,
        CancellationToken cancellationToken
    )
    {
        var prompt = new SelectionPrompt<string>()
            .Title($"[{titleColor}]Select server[/]")
            .SearchPlaceholderText($"[{Colors.Gray}](Type to filter): [/]")
            .AddChoices(choice.Servers)
            .UseConverter(x => x.EscapeMarkup())
            .WrapAround()
            .UseSearchFilter((x, search) => x.Contains(search, StringComparison.OrdinalIgnoreCase));
        ;

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withNavigation: true, withCancel: true),
            cancellationToken
        );
    }

    public async Task<
        PromptResultRecord<(DateTimeOffset StartTime, DateTimeOffset EndTime)>
    > GetSearchTimes(CancellationToken cancellationToken)
    {
        var boundary = _requestPreparation.CaptureTimeBoundary();
        var prompt = new SelectionPrompt<int>()
            .Title($"[{titleColor}]Select search interval[/]")
            .PageSize(15)
            .AddChoices([
#if DEBUG
                60 * 24 * 365 * 5,
#endif
                -1,
                15,
                60,
                60 * 2,
                60 * 3,
                60 * 6,
                60 * 12,
                60 * 24,
                60 * 24 * 2,
                60 * 24 * 5,
                60 * 24 * 7,
            ])
            .WrapAround()
            .UseConverter(value =>
                value switch
                {
                    <= 0 => "Custom",
                    <= 60 => $"Last {value} minutes",
                    > 60 * 24 => $"Last {value / (60 * 24)} days",
                    > 60 => $"Last {value / 60} hours",
                }
            );

        var searchIntervalChoice = await _console
            .Prompt(
                () => _promptAdapter.Setup(prompt, withNavigation: true, withCancel: true),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (searchIntervalChoice.Result == PromptResult.Cancel)
        {
            return PromptResult.Cancel;
        }

        if (searchIntervalChoice.Value != -1)
        {
            var presetEndTime = boundary.LatestSearchTime.AddTicks(-1);
            return (
                presetEndTime.Subtract(TimeSpan.FromMinutes(searchIntervalChoice.Value)),
                presetEndTime
            );
        }

        var startTimePrompt = new TextPrompt<DateTimeOffset>(
            $"[{titleColor}]Enter search start time[/]"
        )
            .DefaultValue(boundary.LatestSearchTime.Subtract(_requestPreparation.SearchDelay))
            .WithConverter(x => x.ToString("yyyy-MM-dd HH:mm:ss"))
            .Validate(date =>
            {
                var error = _requestPreparation.GetStartTimeValidationError(date, boundary);
                return error is null ? ValidationResult.Success() : ValidationResult.Error(error);
            });

        var startTime = await _console
            .Prompt(
                () => _promptAdapter.Setup(startTimePrompt, withNavigation: true, withCancel: true),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (startTime.Result == PromptResult.Cancel)
        {
            return PromptResult.Cancel;
        }

        var endTimePrompt = new TextPrompt<DateTimeOffset>(
            $"[{titleColor}]Enter search end time[/]"
        )
            .DefaultValue(boundary.LatestSearchTime.AddTicks(-1))
            .WithConverter(x => x.ToString("yyyy-MM-dd HH:mm:ss"))
            .Validate(date =>
            {
                var error = _requestPreparation.GetEndTimeValidationError(
                    startTime.Value,
                    date,
                    boundary
                );
                return error is null ? ValidationResult.Success() : ValidationResult.Error(error);
            });

        var endTime = await _console
            .Prompt(
                () => _promptAdapter.Setup(endTimePrompt, withNavigation: true, withCancel: true),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (endTime.Result == PromptResult.Cancel)
        {
            return PromptResult.Cancel;
        }

        return (startTime.Value, endTime.Value);
    }

    public Task<PromptResultRecord<string>> GetSearchPattern(CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>(
            $"[{titleColor}]Search pattern to search for:[/]"
        ).Validate(pattern =>
        {
            var error = _requestPreparation.GetSearchPatternValidationError(pattern);
            return error is null ? ValidationResult.Success() : ValidationResult.Error(error);
        });

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withNavigation: true, withCancel: true),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<WinServerSearchStrategy>> GetServerSearchStrategy(
        bool allowArchiveSearch,
        CancellationToken cancellationToken
    )
    {
        var prompt = new SelectionPrompt<WinServerSearchStrategy>()
            .Title($"[{titleColor}]Select search strategy[/]")
            .PageSize(15)
            .AddChoices([WinServerSearchStrategy.LiveServersOnly])
            .WrapAround()
            .UseConverter(value => value.ToDisplay());

        if (allowArchiveSearch)
        {
            prompt.AddChoices([
                WinServerSearchStrategy.ArchiveServersOnly,
                WinServerSearchStrategy.LiveAndArchiveServers,
            ]);
        }

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withNavigation: true, withCancel: true),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<SearchOptionChoice>> GetHistoryOrRunChoice(
        CancellationToken cancellationToken
    )
    {
        var prompt = new SelectionPrompt<SearchOptionChoice>()
            .Title($"[{titleColor}]Select a command[/]")
            .AddChoices([SearchOptionChoice.NewRun, SearchOptionChoice.FromHistory])
            .UseConverter(x =>
                x switch
                {
                    SearchOptionChoice.FromHistory => "View History",
                    SearchOptionChoice.NewRun => "New Run",
                    _ => throw new NotSupportedException(),
                }
            )
            .WrapAround();

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withNavigation: true),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<WinServerSearchHistoryEntry>> SelectFromSearchHistoryEntries(
        IReadOnlyList<WinServerSearchHistoryEntry> choices,
        CancellationToken cancellationToken
    )
    {
        var orderedRecords = choices
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Payload.ApplicationName)
            .ToList();
        const int maxShownLength = 50;
        var prompt = new SelectionPrompt<WinServerSearchHistoryEntry>()
            .Title($"[{titleColor}]Select history entry[/]")
            .SearchPlaceholderText($"[{Colors.Gray}](Type to filter): [/]")
            .AddChoices(orderedRecords)
            .UseConverter(x =>
                $"{x.CreatedAt:yyyy-MM-dd HH:mm:ss} - {x.Payload.ApplicationName.EscapeMarkup()} - {(x.Payload.SearchPattern.Length <= maxShownLength ? x.Payload.SearchPattern : $"{x.Payload.SearchPattern[..maxShownLength]}...").EscapeMarkup()}"
            )
            .UseSearchFilter(
                (x, s) =>
                    x.Payload.ApplicationName.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || x.Payload.SearchPattern.Contains(s, StringComparison.OrdinalIgnoreCase)
            )
            .WrapAround();

        return _console.Prompt(
            () => _promptAdapter.Setup(prompt, withNavigation: true, withCancel: true),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<FileTraversal>> GetFileTraversal(
        DirectoryInfo currentDirectoryInfo,
        ICollection<FileTraversal> choices,
        CancellationToken cancellationToken
    )
    {
        var prompt = new SelectionPrompt<FileTraversal>()
            .Title($"[{titleColor}]{currentDirectoryInfo.FullName.EscapeMarkup()}[/]")
            .PageSize(15)
            .AddChoices(choices)
            .SearchPlaceholderText($"[{Colors.Gray}](Type to filter): [/]")
            .MoreChoicesText(string.Empty)
            .WrapAround(false)
            .UseSearchFilter(
                (x, search) =>
                    !x.MoveUp
                    && x.FileSystemInfo.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
            )
            .UseConverter(x =>
                x.FileSystemInfo switch
                {
                    null => "..",
                    DirectoryInfo directoryInfo => directoryInfo.Name.EscapeMarkup(),
                    FileInfo fileInfo =>
                        $"{fileInfo.Name.EscapeMarkup()} | {fileInfo.LastWriteTime:dd-MM-yyyy HH:mm:ss.fff} | {fileInfo.Length.ToReadableFileSize()}",
                    _ => throw new InvalidOperationException(),
                }
            );

        return _console.Prompt(
            () =>
                _promptAdapter.Setup(
                    prompt,
                    customEvents: [new OpenInExplorerEvent(currentDirectoryInfo.FullName)],
                    withCancel: true,
                    withNavigation: true
                ),
            cancellationToken
        );
    }
}
