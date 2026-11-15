using Serara.Console;
using Serara.Files;
using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class FileExplorerWorkflow
{
    private readonly WinServerConsoleInput _input;
    private readonly IConsoleInput _consoleInput;
    private readonly IMultiViewConsole _console;
    private readonly MetadataProvider _metadataProvider;
    private readonly IWinApplicationPathProvider _pathProvider;
    private readonly FileSystemEnumerator _fileEnumerator;
    private readonly LogFileExploreFilter _fileFilter = new();

    public FileExplorerWorkflow(
        WinServerConsoleInput input,
        IConsoleInput consoleInput,
        IMultiViewConsole console,
        MetadataProvider metadataProvider,
        IWinApplicationPathProvider pathProvider,
        FileSystemEnumerator fileEnumerator
    )
    {
        _input = input;
        _consoleInput = consoleInput;
        _console = console;
        _metadataProvider = metadataProvider;
        _pathProvider = pathProvider;
        _fileEnumerator = fileEnumerator;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        var context = new BrowseContext();
        var screen = BrowseScreen.Application;

        // Browsing has no normal completion. Prompt cancellation navigates back;
        // cancellation of the supplied token ends the workflow.
        while (true)
        {
            switch (screen)
            {
                case BrowseScreen.Application:
                {
                    var result = await GetApplication(context, cancellationToken)
                        .ConfigureAwait(false);
                    screen = result switch
                    {
                        PromptResult.Success => BrowseScreen.Server,
                        PromptResult.Cancel => BrowseScreen.Application,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case BrowseScreen.Server:
                {
                    var selection = await GetServer(context, cancellationToken)
                        .ConfigureAwait(false);
                    screen = selection switch
                    {
                        ServerSelection.WithArchive => BrowseScreen.ArchiveChoice,
                        ServerSelection.LiveOnly => BrowseScreen.Directory,
                        ServerSelection.Back => BrowseScreen.Application,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case BrowseScreen.ArchiveChoice:
                {
                    var result = await ChooseArchive(context, cancellationToken)
                        .ConfigureAwait(false);
                    screen = result switch
                    {
                        PromptResult.Success => BrowseScreen.Directory,
                        PromptResult.Cancel => BrowseScreen.Server,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                case BrowseScreen.Directory:
                {
                    var navigation = await BrowseDirectory(context, cancellationToken)
                        .ConfigureAwait(false);
                    screen = navigation switch
                    {
                        DirectoryNavigation.Continue => BrowseScreen.Directory,
                        DirectoryNavigation.Back when context.HasArchiveServer =>
                            BrowseScreen.ArchiveChoice,
                        DirectoryNavigation.Back => BrowseScreen.Server,
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                }
                default:
                    throw new InvalidOperationException($"Unknown browsing screen: {screen}");
            }
        }
    }

    private async Task<PromptResult> GetApplication(
        BrowseContext context,
        CancellationToken cancellationToken
    )
    {
        var metadata = await _metadataProvider.GetMetadata(cancellationToken).ConfigureAwait(false);
        var (result, application) = await _input
            .GetApplicationForFileExplorer(metadata, cancellationToken)
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

        context.Application = application;
        return result;
    }

    private async Task<ServerSelection> GetServer(
        BrowseContext context,
        CancellationToken cancellationToken
    )
    {
        var (result, server) = await _input
            .GetServerForFileExplorer(context.Application, cancellationToken)
            .ConfigureAwait(false);
        if (result == PromptResult.Cancel)
        {
            return ServerSelection.Back;
        }

        context.Server = server;
        context.RootPath = null;
        context.CurrentDirectory = null;
        context.IsArchive = false;
        context.HasArchiveServer =
            _pathProvider.SupportsArchiveSearch
            && _pathProvider.SupportsArchive(context.Application);
        if (result != PromptResult.Success)
        {
            throw new InvalidOperationException();
        }

        return context.HasArchiveServer ? ServerSelection.WithArchive : ServerSelection.LiveOnly;
    }

    private async Task<PromptResult> ChooseArchive(
        BrowseContext context,
        CancellationToken cancellationToken
    )
    {
        var (result, isArchive) = await _consoleInput
            .GetUserConfirmation(
                $"[{Colors.Title}]Explore archive server?[/]",
                defaultValue: true,
                withNavigation: true,
                withCancel: true,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        if (result != PromptResult.Cancel)
        {
            context.IsArchive = isArchive;
            context.RootPath = null;
            context.CurrentDirectory = null;
        }

        return result;
    }

    private async Task<DirectoryNavigation> BrowseDirectory(
        BrowseContext context,
        CancellationToken cancellationToken
    )
    {
        var basePath = context.RootPath ??= Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(
                context.IsArchive
                    ? _pathProvider.GetArchiveServerLogPath(context.Application, context.Server)
                    : _pathProvider.GetLiveServerLogPath(context.Application, context.Server)
            )
        );
        context.CurrentDirectory ??= new DirectoryInfo(basePath);

        var isRoot = Path.TrimEndingDirectorySeparator(context.CurrentDirectory.FullName)
            .Equals(basePath, StringComparison.OrdinalIgnoreCase);
        var files = _fileEnumerator.EnumerateFilesToFileSystemInfo(
            context.CurrentDirectory.FullName,
            _fileFilter,
            cancellationToken
        );
        var choices = new List<FileTraversal> { new(true, null!) };
        choices.AddRange(files.Select(file => new FileTraversal(false, file)));

        var (result, traversal) = await _input
            .GetFileTraversal(context.CurrentDirectory, choices, cancellationToken)
            .ConfigureAwait(false);
        if (result == PromptResult.Cancel)
        {
            traversal = new FileTraversal(true, null!);
        }

        var (moveUp, entry) = traversal;
        if (moveUp)
        {
            if (isRoot || context.CurrentDirectory.Parent is null)
            {
                return DirectoryNavigation.Back;
            }

            context.CurrentDirectory = context.CurrentDirectory.Parent;
            return DirectoryNavigation.Continue;
        }

        if (entry is DirectoryInfo directory)
        {
            context.CurrentDirectory = directory;
            return DirectoryNavigation.Continue;
        }

        if (entry is not FileInfo file)
        {
            throw new InvalidOperationException("Invalid type for fileSystemInfo");
        }

        if (file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            Utils.OpenWithZipFileViewer(file.FullName);
        }
        else
        {
            Utils.OpenWithFileEditor(file.FullName);
        }

        return DirectoryNavigation.Continue;
    }

    private enum BrowseScreen
    {
        Application,
        Server,
        ArchiveChoice,
        Directory,
    }

    private enum ServerSelection
    {
        Back,
        LiveOnly,
        WithArchive,
    }

    private enum DirectoryNavigation
    {
        Continue,
        Back,
    }

    private sealed class BrowseContext
    {
        public WinApplicationMetadata Application { get; set; } = null!;
        public string Server { get; set; } = null!;
        public bool IsArchive { get; set; }
        public bool HasArchiveServer { get; set; }
        public string? RootPath { get; set; }
        public DirectoryInfo? CurrentDirectory { get; set; }
    }
}
