using Serara.Tui;
using Spectre.Console;

namespace Serara.Console;

internal sealed class PromptAdapter : IPromptAdapter
{
    private readonly KeyRegistrations _keyRegistrations;

    public PromptAdapter(KeyRegistrations keyRegistrations)
    {
        _keyRegistrations = keyRegistrations;
    }

    private InternalPromptResultRecord<T> HandleCustomHotkeyInvocationException<T>(
        CustomHotkeyInvocationException exception,
        IReadOnlyList<ICustomPromptEvent>? customEvents
    )
        where T : notnull
    {
        if (exception.Key == _keyRegistrations.CancelPress.Key)
        {
            return new(InternalPromptResult.Cancel, default!, default!);
        }

        var match = _keyRegistrations.Mapping.NavigationMapping.FirstOrDefault(x =>
            x.CustomKeyPress.Key == exception.Key
        );

        if (match != default)
        {
            return new(
                InternalPromptResult.CustomAction,
                default!,
                new NavigateToPageCustomAction(match.ViewId)
            );
        }

        CustomPromptAction action = exception.Key switch
        {
            _ when exception.Key == _keyRegistrations.MoveNextPress.Key =>
                new MoveToPageCustomAction(IsNext: true),
            _ when exception.Key == _keyRegistrations.MovePrevPress.Key =>
                new MoveToPageCustomAction(IsNext: false),
            _ when exception.Key == _keyRegistrations.OpenInExplorerPress.Key =>
                new CustomEventAction(customEvents!.Single()),
            _ when exception.Key == _keyRegistrations.RefreshListPress.Key =>
                new RefreshCustomAction(),
            _ => throw new InvalidOperationException(),
        };

        return new(InternalPromptResult.CustomAction, default!, action);
    }

    public InternalPromptResultRecord<T> Setup<T>(
        TextPrompt<T> prompt,
        IReadOnlyList<ICustomPromptEvent>? customEvents = null,
        bool withCancel = false,
        bool withNavigation = false
    )
        where T : notnull
    {
        try
        {
            if (withNavigation)
            {
                foreach (var (_, keyMapping) in _keyRegistrations.Mapping.NavigationMapping)
                {
                    prompt.AddCustomHotkeyRegistration(keyMapping.Key, keyMapping.Func);
                }

                prompt
                    .AddCustomHotkeyRegistration(
                        _keyRegistrations.MoveNextPress.Key,
                        _keyRegistrations.MoveNextPress.Func
                    )
                    .AddCustomHotkeyRegistration(
                        _keyRegistrations.MovePrevPress.Key,
                        _keyRegistrations.MovePrevPress.Func
                    );
            }

            if (withCancel)
            {
                prompt.AddCustomHotkeyRegistration(
                    _keyRegistrations.CancelPress.Key,
                    _keyRegistrations.CancelPress.Func
                );
            }

            var value = AnsiConsole.Prompt(prompt);

            return new(InternalPromptResult.Success, value);
        }
        catch (CustomHotkeyInvocationException ex)
        {
            return HandleCustomHotkeyInvocationException<T>(ex, customEvents);
        }
    }

    public InternalPromptResultRecord<List<T>> Setup<T>(
        MultiSelectionPrompt<T> prompt,
        IReadOnlyList<ICustomPromptEvent>? customEvents = null,
        bool withCancel = false,
        bool withNavigation = false,
        bool withRefresh = false
    )
        where T : notnull
    {
        try
        {
            if (withNavigation)
            {
                foreach (var (_, keyMapping) in _keyRegistrations.Mapping.NavigationMapping)
                {
                    prompt.AddCustomHotkeyRegistration(keyMapping.Key, keyMapping.Func);
                }

                prompt
                    .AddCustomHotkeyRegistration(
                        _keyRegistrations.MoveNextPress.Key,
                        _keyRegistrations.MoveNextPress.Func
                    )
                    .AddCustomHotkeyRegistration(
                        _keyRegistrations.MovePrevPress.Key,
                        _keyRegistrations.MovePrevPress.Func
                    );
            }

            if (withCancel)
            {
                prompt.AddCustomHotkeyRegistration(
                    _keyRegistrations.CancelPress.Key,
                    _keyRegistrations.CancelPress.Func
                );
            }

            if (withRefresh)
            {
                prompt.AddCustomHotkeyRegistration(
                    _keyRegistrations.RefreshListPress.Key,
                    _keyRegistrations.RefreshListPress.Func
                );
            }

            var value = AnsiConsole.Prompt(prompt);

            return new(InternalPromptResult.Success, value);
        }
        catch (CustomHotkeyInvocationException ex)
        {
            var t = HandleCustomHotkeyInvocationException<T>(ex, customEvents);
            return new(t.Result, default!, t.CustomAction);
        }
    }

    public InternalPromptResultRecord<T> Setup<T>(
        SelectionPrompt<T> prompt,
        IReadOnlyList<ICustomPromptEvent>? customEvents = null,
        bool withCancel = false,
        bool withNavigation = false,
        bool withRefresh = false
    )
        where T : notnull
    {
        try
        {
            if (withNavigation)
            {
                foreach (var (_, keyMapping) in _keyRegistrations.Mapping.NavigationMapping)
                {
                    prompt.AddCustomHotkeyRegistration(keyMapping.Key, keyMapping.Func);
                }

                prompt
                    .AddCustomHotkeyRegistration(
                        _keyRegistrations.MoveNextPress.Key,
                        _keyRegistrations.MoveNextPress.Func
                    )
                    .AddCustomHotkeyRegistration(
                        _keyRegistrations.MovePrevPress.Key,
                        _keyRegistrations.MovePrevPress.Func
                    );
            }

            if (withCancel)
            {
                prompt.AddCustomHotkeyRegistration(
                    _keyRegistrations.CancelPress.Key,
                    _keyRegistrations.CancelPress.Func
                );
            }

            if (withRefresh)
            {
                prompt.AddCustomHotkeyRegistration(
                    _keyRegistrations.RefreshListPress.Key,
                    _keyRegistrations.RefreshListPress.Func
                );
            }

            if (customEvents?.Count > 0)
            {
                prompt.AddCustomHotkeyRegistration(
                    _keyRegistrations.OpenInExplorerPress.Key,
                    _keyRegistrations.OpenInExplorerPress.Func
                );
            }

            var value = AnsiConsole.Prompt(prompt);

            return new(InternalPromptResult.Success, value);
        }
        catch (CustomHotkeyInvocationException ex)
        {
            return HandleCustomHotkeyInvocationException<T>(ex, customEvents);
        }
    }
}
