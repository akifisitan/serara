namespace Serara.Module.WinServer.Console;

internal abstract class StateMachineState<TReturn>
{
    public TReturn? ReturnValue { get; private set; }
    public bool IsFinished => ReturnValue is not null;

    public void Finish(TReturn value)
    {
        if (IsFinished)
        {
            throw new InvalidOperationException();
        }

        ReturnValue = value;
    }
}
