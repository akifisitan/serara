using Serara.Files;

namespace Serara.Module.WinServer;

public enum WinServerSearchRequestField
{
    Application,
    Servers,
    SearchStrategy,
    TimeRange,
    SearchPattern,
}

public sealed record WinServerSearchValidationError(
    WinServerSearchRequestField Field,
    string Message
);

public sealed record WinServerSearchRequestPreparationResult(
    WinServerSearchRequest? Request,
    IReadOnlyList<WinServerSearchValidationError> Errors
)
{
    public bool IsValid => Request is not null && Errors.Count == 0;
}

public sealed record WinServerSearchTimeBoundary(
    DateTimeOffset CapturedNow,
    DateTimeOffset LatestSearchTime
);

public sealed class WinServerSearchRequestPreparation
{
    private readonly IWinApplicationPathProvider _pathProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly WinServerSearchOptions _options;

    public WinServerSearchRequestPreparation(
        IWinApplicationPathProvider pathProvider,
        ITimeProvider timeProvider,
        IOptions<WinServerSearchOptions> options
    )
    {
        _pathProvider = pathProvider;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public TimeSpan SearchDelay => TimeSpan.FromMinutes(_options.SearchDelayMinutes);

    public int MinimumSearchPatternLength => _options.MinimumSearchPatternLength;

    public WinServerSearchTimeBoundary CaptureTimeBoundary()
    {
        var now = _timeProvider.LocalNow();
        return new(now, now - SearchDelay);
    }

    public bool CanSearchArchive(WinApplicationMetadata application) =>
        _pathProvider.SupportsArchiveSearch && _pathProvider.SupportsArchive(application);

    public string? GetSearchPatternValidationError(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return "Search pattern must not be empty";
        }

        return pattern.Length < MinimumSearchPatternLength
            ? $"Search pattern must be at least {MinimumSearchPatternLength} characters"
            : null;
    }

    public string? GetStartTimeValidationError(
        DateTimeOffset startTime,
        WinServerSearchTimeBoundary boundary
    ) =>
        startTime >= boundary.LatestSearchTime
            ? $"Start time must be earlier than {SearchDelay.TotalMinutes:g} minutes ago"
            : null;

    public string? GetEndTimeValidationError(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        WinServerSearchTimeBoundary boundary
    )
    {
        if (endTime <= startTime)
        {
            return "End time must be later than start time";
        }

        return endTime >= boundary.LatestSearchTime
            ? $"End time must be earlier than {SearchDelay.TotalMinutes:g} minutes ago"
            : null;
    }

    public WinServerSearchRequestPreparationResult Prepare(
        WinApplicationMetadata? application,
        IReadOnlyCollection<string>? selectedServers,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? searchPattern,
        WinServerSearchStrategy searchStrategy
    )
    {
        var boundary = CaptureTimeBoundary();
        List<WinServerSearchValidationError> errors = [];

        if (application is null)
        {
            errors.Add(
                new(WinServerSearchRequestField.Application, "An application must be selected")
            );
        }

        if (selectedServers is null || selectedServers.Count == 0)
        {
            errors.Add(
                new(WinServerSearchRequestField.Servers, "At least one server must be selected")
            );
        }
        else if (
            application is not null
            && selectedServers.Any(selectedServer =>
                !application.Servers.Contains(selectedServer, StringComparer.OrdinalIgnoreCase)
            )
        )
        {
            errors.Add(
                new(
                    WinServerSearchRequestField.Servers,
                    "Every selected server must belong to the selected application"
                )
            );
        }

        if (!Enum.IsDefined(searchStrategy) || searchStrategy == WinServerSearchStrategy.Default)
        {
            errors.Add(
                new(
                    WinServerSearchRequestField.SearchStrategy,
                    "A server search strategy must be selected"
                )
            );
        }
        else if (
            application is not null
            && searchStrategy
                is WinServerSearchStrategy.ArchiveServersOnly
                    or WinServerSearchStrategy.LiveAndArchiveServers
            && !CanSearchArchive(application)
        )
        {
            errors.Add(
                new(
                    WinServerSearchRequestField.SearchStrategy,
                    "Archive search is not available for the selected application"
                )
            );
        }

        var startError = GetStartTimeValidationError(startTime, boundary);
        if (startError is not null)
        {
            errors.Add(new(WinServerSearchRequestField.TimeRange, startError));
        }

        var endError = GetEndTimeValidationError(startTime, endTime, boundary);
        if (endError is not null)
        {
            errors.Add(new(WinServerSearchRequestField.TimeRange, endError));
        }

        var patternError = GetSearchPatternValidationError(searchPattern);
        if (patternError is not null)
        {
            errors.Add(new(WinServerSearchRequestField.SearchPattern, patternError));
        }

        if (
            errors.Count > 0
            || application is null
            || selectedServers is null
            || searchPattern is null
        )
        {
            return new(null, errors);
        }

        return new(
            new WinServerSearchRequest(
                application,
                [.. selectedServers],
                startTime,
                endTime,
                searchPattern,
                searchStrategy
            ),
            errors
        );
    }

    public WinServerSearchRequestPreparationResult Validate(WinServerSearchRequest request) =>
        Prepare(
            request.SelectedData,
            request.SelectedServers,
            request.StartTime,
            request.EndTime,
            request.SearchPattern,
            request.SearchStrategy
        );
}
