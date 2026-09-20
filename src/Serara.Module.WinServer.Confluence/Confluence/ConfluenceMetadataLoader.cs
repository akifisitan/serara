using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Serara.Infra.Confluence;

namespace Serara.Module.WinServer;

public sealed partial class ConfluenceMetadataLoader : IWinApplicationMetadataLoader
{
    private readonly IConfluenceHttpClientFactory _confluenceHttpClientFactory;
    private readonly ILogger<ConfluenceMetadataLoader> _logger;
    private readonly ConfluenceMetadataLoaderOptions _options;

    public ConfluenceMetadataLoader(
        IConfluenceHttpClientFactory confluenceHttpClientFactory,
        ILogger<ConfluenceMetadataLoader> logger,
        IOptions<ConfluenceMetadataLoaderOptions> options
    )
    {
        _confluenceHttpClientFactory = confluenceHttpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<WinApplicationMetadata>> Load(
        CancellationToken cancellationToken
    )
    {
        var data = await GetPageContent(cancellationToken).ConfigureAwait(false);

        try
        {
            return ScrapeMetadata(data);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while scraping metadata");
            throw new ConfluenceInvalidPageFormatError();
        }
    }

    private async Task<string> GetPageContent(CancellationToken cancellationToken)
    {
        using var httpClient = _confluenceHttpClientFactory.CreateClient();

        var query = HttpUtility.ParseQueryString(string.Empty);

        query.Add("expand", "body.view");

        var uriBuilder = new UriBuilder(
            $"{httpClient.BaseAddress}confluence/rest/api/content/{_options.PageId}"
        )
        {
            Query = query.ToString(),
        };

        HttpResponseMessage? response = null;

        try
        {
            _logger.ZLogInformation($"Sending request to {uriBuilder.Uri}");

            response = await httpClient
                .GetAsync(uriBuilder.Uri, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ConfluenceTimeoutException();
        }

        using var responseScope = response;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ConfluenceInvalidCredentialError();
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new ConfluenceUnauthorizedError();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ConfluencePageNotFoundError();
        }

        response.EnsureSuccessStatusCode();

        var content = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        var result =
            JsonSerializer.Deserialize(
                content,
                ConfluenceJsonSerializerContext.Default.SearchResponse
            )
            ?? throw new JsonException(
                $"An error occurred while deserializing {content} into {nameof(ConfluenceJsonSerializerContext.Default.SearchResponse)}"
            );

        return result.Body.View.Value;
    }

    private List<WinApplicationMetadata> ScrapeMetadata(string pageContent)
    {
        var html = new HtmlDocument();
        html.LoadHtml(pageContent);

        List<WinApplicationMetadata> result = [];

        var firstRow = true;

        Dictionary<string, int> headerMap = [];

        _logger.ZLogInformation($"Started scraping page content for metadata");

        foreach (
            var (tableRow, rowNumber) in html
                .DocumentNode.Descendants("tr")
                .Select((x, i) => (x, i))
        )
        {
            if (firstRow)
            {
                var headers = tableRow
                    .Descendants("th")
                    .Select(x => WebUtility.HtmlDecode(x.InnerText.Trim()))
                    .ToList();

                for (var i = 0; i < headers.Count; i++)
                {
                    headerMap.Add(headers[i], i);
                }

                firstRow = false;
                continue;
            }

            var rowData = tableRow
                .Descendants("td")
                .Select(x => WebUtility.HtmlDecode(x.InnerText).Trim())
                .ToList();

            var uk = GetColumnValueFromRowData(rowData, headerMap, _options.UK);
            var aci = GetColumnValueFromRowData(rowData, headerMap, _options.ACI);
            var applicationName = GetColumnValueFromRowData(
                rowData,
                headerMap,
                _options.ApplicationName
            );
            var teamName =
                GetColumnValueFromRowData(rowData, headerMap, _options.TeamName) ?? string.Empty;

            if (
                IsNullWhiteSpaceOrDefaultValue(uk)
                || IsNullWhiteSpaceOrDefaultValue(aci)
                || IsNullWhiteSpaceOrDefaultValue(applicationName)
            )
            {
                _logger.ZLogWarning(
                    $"""Skipping invalid {nameof(uk)}: "{uk}", {nameof(aci)}: "{aci}", {nameof(applicationName)}: "{applicationName}" in row {rowNumber}: {string.Join("|", rowData)}"""
                );
                continue;
            }

            var serversText = GetColumnValueFromRowData(rowData, headerMap, _options.Servers);

            if (IsNullWhiteSpaceOrDefaultValue(serversText))
            {
                _logger.ZLogWarning(
                    $"""Skipping invalid {nameof(serversText)}: "{serversText}" in row {rowNumber}: {string.Join("|", rowData)}"""
                );
                continue;
            }

            var servers = new List<string>();
            try
            {
                servers = ParseServers(serversText);
            }
            catch (FormatException ex)
            {
                _logger.ZLogWarning(
                    ex,
                    $"Skipping invalid servers in row {rowNumber}: {serversText}"
                );
                continue;
            }

            if (servers.Count == 0)
            {
                _logger.ZLogWarning(
                    $"""Skipping empty {nameof(servers)}: "{serversText}" in row {rowNumber}: {string.Join("|", rowData)}"""
                );
                continue;
            }

            var logPath = GetColumnValueFromRowData(rowData, headerMap, _options.LogPath);
            if (IsNullWhiteSpaceOrDefaultValue(logPath))
            {
                _logger.ZLogWarning(
                    $"""Skipping invalid {nameof(logPath)}: "{logPath}" in row {rowNumber}: {string.Join("|", rowData)}"""
                );
                continue;
            }

            var archiveDirectoryName = GetColumnValueFromRowData(
                rowData,
                headerMap,
                _options.ArchiveDirectoryName
            );

            if (IsNullWhiteSpaceOrDefaultValue(archiveDirectoryName))
            {
                archiveDirectoryName = string.Empty;
            }

            var searchTerm = GetColumnValueFromRowData(rowData, headerMap, _options.SearchTerm);

            if (IsNullWhiteSpaceOrDefaultValue(searchTerm))
            {
                searchTerm = string.Empty;
            }

            var logSearchPattern = GetColumnValueFromRowData(
                rowData,
                headerMap,
                _options.LogSearchPattern
            );

            if (IsNullWhiteSpaceOrDefaultValue(logSearchPattern))
            {
                logSearchPattern = string.Empty;
            }

            result.Add(
                new WinApplicationMetadata
                {
                    TeamName = teamName,
                    UK = uk,
                    ACI = aci,
                    Name = applicationName,
                    Servers = servers,
                    LogPath = logPath,
                    LogSearchPattern = logSearchPattern,
                    SearchTerm = searchTerm,
                    ArchiveDirectoryName = archiveDirectoryName,
                }
            );
        }

        return result;
    }

    private static string? GetColumnValueFromRowData(
        List<string> rowData,
        Dictionary<string, int> headerMap,
        string key
    )
    {
        var index = headerMap.GetValueOrDefault(key, int.MaxValue);

        return rowData.ElementAtOrDefault(index);
    }

    private static bool IsNullWhiteSpaceOrDefaultValue([NotNullWhen(false)] string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Length < 3;
    }

    private static List<string> ParseServers(string content)
    {
        var result = new List<string>();

        var serverGroups = content.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        foreach (var serverGroup in serverGroups)
        {
            result.AddRange(ExpandServerGroup(serverGroup));
        }

        return result;
    }

    private static List<string> ExpandServerGroup(string serverGroup)
    {
        var match = ServerRangeRegex().Match(serverGroup);
        if (!match.Success)
        {
            return [serverGroup];
        }

        var prefix = match.Groups["prefix"].Value;
        var endPrefix = match.Groups["endPrefix"].Value;
        if (endPrefix.Length > 0 && !prefix.Equals(endPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return [serverGroup];
        }

        var startText = match.Groups["start"].Value;
        var endText = match.Groups["end"].Value;
        if (
            !int.TryParse(startText, NumberStyles.None, CultureInfo.InvariantCulture, out var start)
            || !int.TryParse(endText, NumberStyles.None, CultureInfo.InvariantCulture, out var end)
            || end < start
            || (long)end - start >= 10_000
        )
        {
            throw new FormatException($"Invalid server range: {serverGroup}");
        }

        var format = startText.Length > 1 && startText[0] == '0' ? $"D{startText.Length}" : "D";
        return Enumerable
            .Range(start, end - start + 1)
            .Select(number => prefix + number.ToString(format, CultureInfo.InvariantCulture))
            .ToList();
    }

    [GeneratedRegex(
        @"^(?<prefix>.*?[^0-9\s])(?<start>[0-9]+)\s*-\s*(?:(?<endPrefix>.*?[^0-9\s]))?(?<end>[0-9]+)$"
    )]
    private static partial Regex ServerRangeRegex();
}
