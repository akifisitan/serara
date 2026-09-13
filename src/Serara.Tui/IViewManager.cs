namespace Serara.Tui;

public interface IViewManager
{
    Task Start(CancellationToken cancellationToken);
    ViewId GetActiveViewId();
    void RegisterView(ViewId viewId);
    void NavigateToView(ViewId viewId);
    void NavigateToViewIfActive(ViewId viewId, ViewId viewIdToSwitchTo);
    void UnregisterView(ViewId viewId);
    void SendMessageToView<T>(ViewId viewId, T message)
        where T : IViewMessage;
    Task<T> ReadMessageFromView<T>(ViewId viewId, CancellationToken cancellationToken)
        where T : IViewMessage;
    void WriteToBufferChannel(BufferChannelItem item);
    Task<PromptResultRecord<T>> ReadFromPromptResultChannel<T>(
        ViewId viewId,
        CancellationToken cancellationToken
    )
        where T : notnull;
    Task<CustomPromptEventResult<T>> ReadFromCustomEventChannel<T>(
        ViewId viewId,
        CancellationToken cancellationToken
    )
        where T : ICustomPromptEvent;
}
