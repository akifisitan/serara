using System.Collections.Concurrent;
using System.Diagnostics;
using Spectre.Console;

namespace Serara.Tui;

internal sealed class ViewManager : IViewManager, IDisposable
{
    private ActiveView _activeView = null!;

    private readonly ConcurrentDictionary<ViewId, ViewData> _viewMap = [];
    private readonly List<ViewId> _viewIdList = [];
    private readonly List<KeyHandlerRegistration> _keyHandlers = [];

    private readonly Lock _consoleLock = new();
    private readonly SemaphoreSlim _startEvent = new(0);
    private const int MaxBufferedItemsPerView = 2_000;
    private const int MaxPendingOutputItems = 2_000;
    private int _pendingOutputItems;

    private readonly ILogger<ViewManager> _logger;

    public ViewManager(ILogger<ViewManager> logger, IKeyRegistrationMapping registrationMapping)
    {
        _logger = logger;
        RegisterKeyHandler(
            null,
            new CustomKeyPress("NextView", registrationMapping.MoveNextPressed),
            () => HandleViewNavigation(isNext: true)
        );
        RegisterKeyHandler(
            null,
            new CustomKeyPress("PreviousView", registrationMapping.MovePrevPressed),
            () => HandleViewNavigation(isNext: false)
        );
        foreach (var (viewId, keyPress) in registrationMapping.NavigationMapping)
        {
            RegisterKeyHandler(null, keyPress, () => NavigateToView(viewId));
        }
    }

    private readonly Channel<BufferChannelItem> _bufferChannel =
        Channel.CreateUnbounded<BufferChannelItem>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true }
        );

    private readonly Channel<SpectrePromptBufferChannelItem> _promptBufferChannel =
        Channel.CreateUnbounded<SpectrePromptBufferChannelItem>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true }
        );

    public async Task Start(CancellationToken cancellationToken)
    {
        // wait for a view to be registered via RegisterView()
        await _startEvent.WaitAsync(cancellationToken).ConfigureAwait(false);

        using var viewCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        var viewNavigatorTask = Task.Run(
            () => ViewNavigator(viewCancellation.Token),
            viewCancellation.Token
        );

        var viewPrinterTask = ViewPrinter(viewCancellation.Token);

        await Task.WhenAny(viewNavigatorTask, viewPrinterTask).ConfigureAwait(false);
        await viewCancellation.CancelAsync().ConfigureAwait(false);
        await Task.WhenAll(viewNavigatorTask, viewPrinterTask).ConfigureAwait(false);
    }

    public void WriteToBufferChannel(BufferChannelItem item)
    {
        lock (_consoleLock)
        {
            if (!_viewMap.TryGetValue(item.ViewId, out var view))
            {
                return;
            }

            if (item is ClearBufferChannelItem)
            {
                view.ConsoleBuffer.RemoveAll(x =>
                    x is not SpectrePromptBufferChannelItem { IsComplete: false }
                );
            }
            else
            {
                view.ConsoleBuffer.Add(item);
                while (view.ConsoleBuffer.Count > MaxBufferedItemsPerView)
                {
                    var removableIndex = view.ConsoleBuffer.FindIndex(x =>
                        x is not SpectrePromptBufferChannelItem { IsComplete: false }
                    );
                    if (removableIndex < 0)
                    {
                        break;
                    }

                    view.ConsoleBuffer.RemoveAt(removableIndex);
                }
            }

            if (_activeView.Id == item.ViewId)
            {
                QueueBufferItem(item);
            }
        }
    }

    private void QueueBufferItem(BufferChannelItem item)
    {
        // Keep prompt and control messages even when output arrives faster than it can be printed.
        if (item is SpectreMarkupBufferChannelItem or SpectreRenderableBufferChannelItem)
        {
            if (_pendingOutputItems >= MaxPendingOutputItems)
            {
                return;
            }

            _pendingOutputItems++;
        }

        _bufferChannel.Writer.TryWrite(item);
    }

    public async Task<PromptResultRecord<T>> ReadFromPromptResultChannel<T>(
        ViewId viewId,
        CancellationToken cancellationToken
    )
        where T : notnull
    {
        var reader = _viewMap[viewId].PromptResultChannel.Reader;
        var record = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return InternalPromptResultRecord<T>.ToPromptResultRecord(record.Result, record.Value);
    }

    public async Task<CustomPromptEventResult<T>> ReadFromCustomEventChannel<T>(
        ViewId viewId,
        CancellationToken cancellationToken
    )
        where T : ICustomPromptEvent
    {
        var reader = _viewMap[viewId].CustomResultChannel.Reader;
        var value = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return new CustomPromptEventResult<T>(value.InsertedAt, (T)value.Event);
    }

    private void ViewNavigator(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var (result, key) = ReadKey(intercept: true, cancellationToken);

            if (result != ReadKeyResult.KeyRead)
            {
                continue;
            }
            lock (_consoleLock)
            {
                if (_activeView is null)
                {
                    continue;
                }

                var registration = _keyHandlers.FirstOrDefault(x =>
                    (x.ViewId is null || x.ViewId == _activeView.Id) && x.KeyPress.Func(key)
                );
                registration?.Handler();
            }
        }
    }

    public ViewId GetActiveViewId()
    {
        lock (_consoleLock)
        {
            return _activeView?.Id ?? throw new InvalidOperationException("No view is registered.");
        }
    }

    public IDisposable RegisterKeyHandler(ViewId? viewId, CustomKeyPress keyPress, Action handler)
    {
        lock (_consoleLock)
        {
            var registration = new KeyHandlerRegistration(this, viewId, keyPress, handler);
            if (viewId is null)
            {
                _keyHandlers.Add(registration);
            }
            else
            {
                // View-specific actions take precedence over global shortcuts.
                _keyHandlers.Insert(0, registration);
            }
            return registration;
        }
    }

    private sealed class KeyHandlerRegistration : IDisposable
    {
        private readonly ViewManager _manager;

        public KeyHandlerRegistration(
            ViewManager manager,
            ViewId? viewId,
            CustomKeyPress keyPress,
            Action handler
        )
        {
            _manager = manager;
            ViewId = viewId;
            KeyPress = keyPress;
            Handler = handler;
        }

        public ViewId? ViewId { get; }
        public CustomKeyPress KeyPress { get; }
        public Action Handler { get; }

        public void Dispose()
        {
            lock (_manager._consoleLock)
            {
                _manager._keyHandlers.Remove(this);
            }
        }
    }

    public void NavigateToViewIfActive(ViewId viewId, ViewId viewIdToSwitchTo)
    {
        if (GetActiveViewId() != viewId)
        {
            return;
        }

        NavigateToView(viewIdToSwitchTo);
    }

    public void NavigateToView(ViewId viewId)
    {
        lock (_consoleLock)
        {
            if (_activeView is null)
            {
                return;
            }
            _activeView.Id = _viewIdList.FirstOrDefault(x => x == viewId) ?? _activeView.Id;

            ClearScreen();

            // Pending output is already retained in each view's history.
            while (_bufferChannel.Reader.TryRead(out _)) { }
            _pendingOutputItems = 0;

            // Replay retained output without adding it to the history again.
            foreach (var item in _viewMap[_activeView.Id].ConsoleBuffer)
            {
                QueueBufferItem(item);
            }
        }
    }

    private void HandleViewNavigation(bool isNext)
    {
        lock (_consoleLock)
        {
            if (_viewIdList.Count == 0)
            {
                return;
            }
            var previousActiveView = _activeView.Id;

            var previousActiveViewIndex = _viewIdList.IndexOf(previousActiveView);

            var newActiveViewIndex = isNext
                ? (
                    previousActiveViewIndex == (_viewIdList.Count - 1)
                        ? 0
                        : previousActiveViewIndex + 1
                )
                : (
                    previousActiveViewIndex == 0
                        ? _viewIdList.Count - 1
                        : previousActiveViewIndex - 1
                );

            var newActiveView = _viewIdList[newActiveViewIndex];

            NavigateToView(newActiveView);
        }
    }

    public void SendMessageToView<T>(ViewId viewId, T message)
        where T : IViewMessage
    {
        _viewMap[viewId].ViewMessageChannel.Writer.TryWrite(message);
    }

    public async Task<T> ReadMessageFromView<T>(ViewId viewId, CancellationToken cancellationToken)
        where T : IViewMessage
    {
        return (T)
            await _viewMap[viewId]
                .ViewMessageChannel.Reader.ReadAsync(cancellationToken)
                .ConfigureAwait(false);
    }

    private bool IsRegistered(ViewId viewId)
    {
        return _viewMap.ContainsKey(viewId);
    }

    public void RegisterView(ViewId viewId, ViewPresentation presentation)
    {
        lock (_consoleLock)
        {
            if (IsRegistered(viewId))
            {
                throw new InvalidOperationException(
                    $"The view with id {viewId} has already been registered"
                );
            }

            _viewIdList.Add(viewId);
            _viewMap.TryAdd(viewId, new ViewData(viewId, presentation));

            if (_activeView is null)
            {
                _activeView = new ActiveView { Id = viewId };
                QueueBufferItem(
                    new CallbackBufferChannelItem { ViewId = viewId, Callback = WriteHeading }
                );
            }
        }

        _startEvent.Release();
    }

    public void UnregisterView(ViewId viewId)
    {
        lock (_consoleLock)
        {
            if (!_viewMap.TryRemove(viewId, out _))
            {
                return;
            }

            _viewIdList.Remove(viewId);
            _keyHandlers.RemoveAll(x => x.ViewId == viewId);
            if (_activeView.Id != viewId)
            {
                return;
            }

            if (_viewIdList.Count > 0)
            {
                NavigateToView(_viewIdList[0]);
            }
            else
            {
                _activeView = null!;
                while (_bufferChannel.Reader.TryRead(out _)) { }
                _pendingOutputItems = 0;
                AnsiConsole.Clear();
            }
        }
    }

    private void WriteHeading()
    {
        if (_activeView is not null && _viewMap.TryGetValue(_activeView.Id, out var view))
        {
            var presentation = view.Presentation;
            AnsiConsole.MarkupLine(
                $"[{presentation.AccentColor}]── {presentation.Title.EscapeMarkup()} ──[/]"
            );
        }
    }

    private void ClearScreen()
    {
        lock (_consoleLock)
        {
            AnsiConsole.Clear();
            WriteHeading();
        }
    }

    private void ConsumeBufferChannel()
    {
        lock (_consoleLock)
        {
            while (_bufferChannel.Reader.TryRead(out var item))
            {
                if (item is SpectreMarkupBufferChannelItem or SpectreRenderableBufferChannelItem)
                {
                    _pendingOutputItems--;
                }

                if (_activeView?.Id == item.ViewId)
                {
                    switch (item)
                    {
                        case SpectrePromptBufferChannelItem x:
                            if (!x.IsComplete && !x.IsQueued)
                            {
                                x.IsQueued = true;
                                _promptBufferChannel.Writer.TryWrite(x);
                            }
                            break;
                        case CallbackBufferChannelItem x:
                            x.Callback();
                            break;
                        case SpectreMarkupBufferChannelItem x:
                            try
                            {
                                AnsiConsole.MarkupLine(x.Text);
                            }
                            catch (Exception ex)
                                when (ex
                                        is InvalidOperationException
                                            or FormatException
                                            or ArgumentException
                                )
                            {
                                _logger.ZLogWarning(
                                    ex,
                                    $"Invalid console markup in view {item.ViewId}"
                                );
                                AnsiConsole.WriteLine(x.Text);
                            }
                            break;
                        case SpectreRenderableBufferChannelItem x:
                            AnsiConsole.Write(x.Renderable);
                            break;
                        case ClearBufferChannelItem:
                            ClearScreen();
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }

    private async Task ViewPrinter(CancellationToken cancellationToken)
    {
        while (await _bufferChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            ConsumeBufferChannel();
        }
    }

    private enum ReadKeyResult
    {
        KeyRead = 0,
        Cancelled = 1,
        PromptHandled = 2,
    }

    private (ReadKeyResult Result, ConsoleKeyInfo Key) ReadKey(
        bool intercept,
        CancellationToken cancellationToken
    )
    {
        const int pollingMs = 5;
        // Spin loop by sleeping until there's a key input
        // Use Console.KeyAvailable + Console.ReadKey() instead of blocking on Console.ReadKey()
        while (!cancellationToken.IsCancellationRequested)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return (ReadKeyResult.Cancelled, default);
            }

            // Give priority to prompts
            if (_promptBufferChannel.Reader.TryRead(out var item))
            {
                lock (_consoleLock)
                {
                    item.IsQueued = false;
                    if (item.IsComplete || _activeView?.Id != item.ViewId)
                    {
                        continue;
                    }
                }

                var (promptResult, value, action) = item.PromptFunc();

                while (
                    promptResult is InternalPromptResult.CustomAction
                    && action is RefreshCustomAction
                )
                {
                    ClearScreen();
                    (promptResult, value, action) = item.PromptFunc();
                }

                while (
                    promptResult is InternalPromptResult.CustomAction
                    && action is CustomEventAction eventAction
                )
                {
                    _viewMap[item.ViewId]
                        .CustomResultChannel.Writer.TryWrite(
                            new CustomPromptEventResult<ICustomPromptEvent>(
                                Stopwatch.GetTimestamp(),
                                eventAction.Event
                            )
                        );
                    ClearScreen();
                    (promptResult, value, action) = item.PromptFunc();
                }

                lock (_consoleLock)
                {
                    if (
                        promptResult is InternalPromptResult.Success
                        || promptResult is InternalPromptResult.Cancel
                    )
                    {
                        if (!item.DoNotMarkAsComplete)
                        {
                            item.SetAsComplete();
                            _viewMap[item.ViewId].ConsoleBuffer.Remove(item);
                        }

                        _viewMap[item.ViewId]
                            .PromptResultChannel.Writer.TryWrite(new(promptResult, value));
                    }
                    else if (
                        promptResult is InternalPromptResult.CustomAction
                        && action is MoveToPageCustomAction moveToPageCustomAction
                    )
                    {
                        HandleViewNavigation(isNext: moveToPageCustomAction.IsNext);
                    }
                    else if (
                        promptResult is InternalPromptResult.CustomAction
                        && action is NavigateToPageCustomAction navigateToPageCustomAction
                    )
                    {
                        NavigateToView(navigateToPageCustomAction.ViewId);
                    }

                    // Flush queued inputs
                    while (Console.KeyAvailable)
                    {
                        _ = Console.ReadKey(intercept);
                    }
                }

                return (ReadKeyResult.PromptHandled, default);
            }

            // Break to read the key
            if (Console.KeyAvailable)
            {
                break;
            }

            Thread.Sleep(pollingMs);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return (ReadKeyResult.Cancelled, default);
        }

        return (ReadKeyResult.KeyRead, Console.ReadKey(intercept));
    }

    public void Dispose()
    {
        _startEvent.Dispose();
    }
}
