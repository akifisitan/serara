namespace Serara.Module.WinServer.Console;

internal sealed class RetrieveFileExplorerOptionsStateMachineState : StateMachineState<Unit>
{
    public WinApplicationMetadata SelectedData { get; set; } = default!;
    public string SelectedServer { get; set; } = default!;
    public bool IsArchive { get; set; }
    public DirectoryInfo CurrentDirectory { get; set; } = default!;
    public bool HasArchiveServer { get; set; }
}
