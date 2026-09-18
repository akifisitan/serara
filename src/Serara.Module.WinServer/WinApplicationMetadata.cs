namespace Serara.Module.WinServer;

public sealed record WinApplicationMetadata
{
    public required string Name { get; init; }
    public required string TeamName { get; init; }
    public required string UK { get; init; }
    public required string ACI { get; init; }
    public required string LogPath { get; init; }
    public required string SearchTerm { get; init; }
    public required List<string> Servers { get; init; }
    public required string LogSearchPattern { get; init; }
    public required string ArchiveDirectoryName { get; init; }
}
