namespace Serara.Module.WinServer.Console;

internal sealed class RetrieveSearchRunOptionsStateMachineRecord
    : StateMachineState<WinServerSearchRequest>
{
    public bool IsOnSearchHistoryBranch { get; set; }
    public WinApplicationMetadata SelectedData { get; set; } = default!;
    public IReadOnlyCollection<string> SelectedServers { get; set; } = default!;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string SearchPattern { get; set; } = default!;
    public WinServerSearchStrategy SearchStrategy { get; set; }
}
