using Serara.Files;
using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class SelectDirectoryForFileExplorerStep
    : BuilderStateMachineStep<RetrieveFileExplorerOptionsStateMachineState, bool>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly FileSystemEnumerator _fileEnumerator;
    private readonly LogFileExploreFilter logFileExploreFilter = new();
    private readonly IWinApplicationPathProvider _pathProvider;

    public SelectDirectoryForFileExplorerStep(
        WinServerConsoleInput consoleInput,
        FileSystemEnumerator fileEnumerator,
        IWinApplicationPathProvider pathProvider
    )
    {
        _consoleInput = consoleInput;
        _fileEnumerator = fileEnumerator;
        _pathProvider = pathProvider;
    }

    protected override async Task<PromptResultRecord<bool>> ExecuteAsync(
        RetrieveFileExplorerOptionsStateMachineState input,
        CancellationToken cancellationToken
    )
    {
        var basePath = input.IsArchive
            ? _pathProvider.GetArchiveServerLogPath(input.SelectedData, input.SelectedServer)
            : _pathProvider.GetLiveServerLogPath(input.SelectedData, input.SelectedServer);

        input.CurrentDirectory ??= new DirectoryInfo(basePath);

        var isRootDir = input.CurrentDirectory.FullName.Equals(
            basePath,
            StringComparison.OrdinalIgnoreCase
        );

        var logFiles = _fileEnumerator.EnumerateFilesToFileSystemInfo(
            input.CurrentDirectory.FullName,
            logFileExploreFilter,
            cancellationToken
        );

        List<FileTraversal> choices = [];

        if (input.CurrentDirectory.Parent is not null && !isRootDir)
        {
            choices.Add(new FileTraversal(true, null!));
        }

        choices.AddRange(logFiles.Select(x => new FileTraversal(false, x)));

        var (result, value) = await _consoleInput
            .GetFileTraversal(input.CurrentDirectory, choices, cancellationToken)
            .ConfigureAwait(false);

        if (result == PromptResult.Cancel)
        {
            value = new FileTraversal(true, default!);
        }

        var (moveUp, fileSystemInfo) = value;

        if (moveUp)
        {
            if (isRootDir || input.CurrentDirectory.Parent is null)
            {
                // break out and go into server selection on escape press
                return new(PromptResult.Cancel, input.HasArchiveServer);
            }

            // Move up directory
            input.CurrentDirectory = input.CurrentDirectory.Parent;
            return PromptResult.Success;
        }

        // Move down directory
        if (fileSystemInfo is DirectoryInfo directoryInfo)
        {
            input.CurrentDirectory = directoryInfo;
            return PromptResult.Success;
        }

        if (fileSystemInfo is not FileInfo fileInfo)
        {
            throw new InvalidOperationException($"Invalid type for {nameof(fileSystemInfo)}");
        }

        // Open file
        if (fileInfo.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            Utils.OpenWithZipFileViewer(fileInfo.FullName);
        }
        else
        {
            Utils.OpenWithFileEditor(fileInfo.FullName);
        }

        return PromptResult.Success;
    }
}
