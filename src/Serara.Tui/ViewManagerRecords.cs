using Spectre.Console.Rendering;

namespace Serara.Tui;

public enum PromptResult
{
    Default = 0,
    Success = 1,
    Cancel = 2,
}

public enum InternalPromptResult
{
    Default = 0,
    Success = 1,
    Cancel = 2,
    CustomAction = 3,
}

public sealed record InternalPromptResultRecord<T>(
    InternalPromptResult Result,
    T Value,
    CustomPromptAction CustomAction = default!
)
{
    public static PromptResultRecord<T> ToPromptResultRecord(
        InternalPromptResult result,
        object value
    )
    {
        return new PromptResultRecord<T>(
            result switch
            {
                InternalPromptResult.Success => PromptResult.Success,
                InternalPromptResult.Cancel => PromptResult.Cancel,
                _ => throw new InvalidOperationException(),
            },
            (T)value
        );
    }
}

public abstract record CustomPromptAction;

public sealed record RefreshCustomAction : CustomPromptAction;

public sealed record CustomEventAction(ICustomPromptEvent Event) : CustomPromptAction;

public sealed record MoveToPageCustomAction(bool IsNext) : CustomPromptAction;

public sealed record NavigateToPageCustomAction(ViewId ViewId) : CustomPromptAction;

public interface ICustomPromptEvent;

public sealed record CustomPromptEventResult<T>(long InsertedAt, T Event)
    where T : ICustomPromptEvent;

public interface IViewMessage;

public sealed record PromptResultRecord<T>(PromptResult Result, T Value)
{
    public static implicit operator PromptResultRecord<T>(PromptResult value)
    {
        return new(value, default!);
    }

    // if T is PromptResultRecord<T> can become PromptResultRecord<PromptResultRecord<T>>
    public static implicit operator PromptResultRecord<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new(PromptResult.Success, value);
    }
}

internal sealed class SpectrePromptBufferChannelItem : BufferChannelItem
{
    public required string PromptId { get; init; }
    public bool IsComplete { get; private set; }
    public bool IsQueued { get; set; }
    public bool DoNotMarkAsComplete { get; init; }

    public required Func<InternalPromptResultRecord<object>> PromptFunc { get; init; } // should not throw

    public void SetAsComplete()
    {
        IsComplete = true;
    }
}

internal sealed class CallbackBufferChannelItem : BufferChannelItem
{
    public required Action Callback { get; init; }
}

internal sealed class SpectreRenderableBufferChannelItem : BufferChannelItem
{
    public required Renderable Renderable { get; init; }
}

internal sealed class SpectreMarkupBufferChannelItem : BufferChannelItem
{
    public required string Text { get; init; }
}

internal sealed class ClearBufferChannelItem : BufferChannelItem;

internal sealed class ViewData
{
    public ViewId ViewId { get; }
    public List<BufferChannelItem> ConsoleBuffer { get; }
    public Channel<InternalPromptResultRecord<object>> PromptResultChannel { get; }
    public Channel<CustomPromptEventResult<ICustomPromptEvent>> CustomResultChannel { get; }
    public Channel<IViewMessage> ViewMessageChannel { get; }

    public ViewData(ViewId viewId)
    {
        ViewId = viewId;
        PromptResultChannel = Channel.CreateUnbounded<InternalPromptResultRecord<object>>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }
        );
        CustomResultChannel = Channel.CreateUnbounded<CustomPromptEventResult<ICustomPromptEvent>>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }
        );
        ViewMessageChannel = Channel.CreateUnbounded<IViewMessage>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }
        );
        ConsoleBuffer = [];
    }
}

public abstract class BufferChannelItem
{
    public required ViewId ViewId { get; init; }
}

internal sealed class ActiveView
{
    public required ViewId Id { get; set; }
}

public sealed record ViewId(string Value);

public interface IKeyRegistrationMapping
{
    bool MoveNextPressed(ConsoleKeyInfo x);
    bool MovePrevPressed(ConsoleKeyInfo x);
    IReadOnlyList<(ViewId ViewId, CustomKeyPress CustomKeyPress)> NavigationMapping { get; }
}

public sealed record CustomKeyPress(string Key, Func<ConsoleKeyInfo, bool> Func);
