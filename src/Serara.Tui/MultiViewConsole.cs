namespace Serara.Tui;

public interface IMultiViewConsole : IConsole
{
    Task<PromptResultRecord<T>> Prompt<T>(
        Func<InternalPromptResultRecord<T>> promptFunc,
        CancellationToken cancellationToken
    )
        where T : notnull;
    Task<CustomPromptEventResult<T>> ReadPromptEvent<T>(CancellationToken cancellationToken)
        where T : ICustomPromptEvent;
}

internal sealed class MultiViewConsole : IMultiViewConsole
{
    private readonly IViewManager _viewManager;
    private readonly IViewContext _viewContext;

    public MultiViewConsole(IViewManager viewManager, IViewContext viewContext)
    {
        _viewManager = viewManager;
        _viewContext = viewContext;
    }

    public void WriteLine(string text)
    {
        const int maxMessageLength = 16_384;
        if (text.Length > maxMessageLength)
        {
            _viewManager.WriteToBufferChannel(
                new SpectreRenderableBufferChannelItem
                {
                    ViewId = _viewContext.ViewId,
                    Renderable = new Spectre.Console.Text(
                        text[..maxMessageLength]
                            + "\n[Output truncated; see logs for full details.]\n"
                    ),
                }
            );
            return;
        }

        _viewManager.WriteToBufferChannel(
            new SpectreMarkupBufferChannelItem { ViewId = _viewContext.ViewId, Text = text }
        );
    }

    public void Clear()
    {
        _viewManager.WriteToBufferChannel(
            new ClearBufferChannelItem { ViewId = _viewContext.ViewId }
        );
    }

    public Task<PromptResultRecord<T>> Prompt<T>(
        Func<InternalPromptResultRecord<T>> promptFunc,
        CancellationToken cancellationToken
    )
        where T : notnull
    {
        _viewManager.WriteToBufferChannel(
            new SpectrePromptBufferChannelItem
            {
                PromptFunc = () =>
                {
                    var (result, value, action) = promptFunc();
                    return new(result, value, action);
                },
                ViewId = _viewContext.ViewId,
                PromptId = Guid.NewGuid().ToString(),
            }
        );

        return _viewManager.ReadFromPromptResultChannel<T>(_viewContext.ViewId, cancellationToken);
    }

    public Task<CustomPromptEventResult<T>> ReadPromptEvent<T>(CancellationToken cancellationToken)
        where T : ICustomPromptEvent
    {
        return _viewManager.ReadFromCustomEventChannel<T>(_viewContext.ViewId, cancellationToken);
    }
}
