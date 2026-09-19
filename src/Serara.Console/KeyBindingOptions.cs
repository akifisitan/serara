using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace Serara.Console;

public sealed record KeyBindingOptions
{
    public KeyChordOptions? NextView { get; set; }
    public KeyChordOptions? PreviousView { get; set; }
    public KeyChordOptions? MainView { get; set; }
    public KeyChordOptions? FileExplorerView { get; set; }
    public KeyChordOptions? OpenLogView { get; set; }
    public KeyChordOptions? HelpView { get; set; }
    public KeyChordOptions? Cancel { get; set; }
    public KeyChordOptions? RefreshResults { get; set; }
    public KeyChordOptions? OpenInExplorer { get; set; }
}

public sealed record KeyChordOptions
{
    public string? Key { get; set; }
    public string? Modifiers { get; set; }
}

internal enum KeyBindingAction
{
    NextView,
    PreviousView,
    MainView,
    FileExplorerView,
    OpenLogView,
    HelpView,
    Cancel,
    RefreshResults,
    OpenInExplorer,
}

internal sealed record KeyChord(ConsoleKey Key, ConsoleModifiers Modifiers)
{
    public bool Matches(ConsoleKeyInfo keyInfo) =>
        keyInfo.Key == Key && keyInfo.Modifiers.HasFlag(Modifiers);

    public string ToDisplayString()
    {
        List<string> parts = [];
        if (Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            parts.Add("Ctrl");
        }
        if (Modifiers.HasFlag(ConsoleModifiers.Alt))
        {
            parts.Add("Alt");
        }
        if (Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            parts.Add("Shift");
        }
        parts.Add(
            Key switch
            {
                >= ConsoleKey.D0 and <= ConsoleKey.D9 => ((int)Key - (int)ConsoleKey.D0).ToString(),
                ConsoleKey.LeftArrow => "Left",
                ConsoleKey.RightArrow => "Right",
                ConsoleKey.UpArrow => "Up",
                ConsoleKey.DownArrow => "Down",
                ConsoleKey.Escape => "Esc",
                _ => Key.ToString(),
            }
        );
        return string.Join(" + ", parts);
    }
}

internal sealed class KeyBindingSet
{
    private readonly IReadOnlyDictionary<KeyBindingAction, KeyChord> _bindings;

    public KeyBindingSet(IOptions<KeyBindingOptions> options)
    {
        var value = options.Value;
        _bindings = Enum.GetValues<KeyBindingAction>()
            .ToDictionary(
                action => action,
                action => Parse(GetOverride(value, action) ?? GetDefault(action))
            );
    }

    public KeyChord this[KeyBindingAction action] => _bindings[action];

    private static KeyChordOptions? GetOverride(
        KeyBindingOptions options,
        KeyBindingAction action
    ) =>
        action switch
        {
            KeyBindingAction.NextView => options.NextView,
            KeyBindingAction.PreviousView => options.PreviousView,
            KeyBindingAction.MainView => options.MainView,
            KeyBindingAction.FileExplorerView => options.FileExplorerView,
            KeyBindingAction.OpenLogView => options.OpenLogView,
            KeyBindingAction.HelpView => options.HelpView,
            KeyBindingAction.Cancel => options.Cancel,
            KeyBindingAction.RefreshResults => options.RefreshResults,
            KeyBindingAction.OpenInExplorer => options.OpenInExplorer,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    internal static KeyChordOptions GetDefault(KeyBindingAction action) =>
        action switch
        {
            KeyBindingAction.NextView => Create(ConsoleKey.RightArrow, ConsoleModifiers.Alt),
            KeyBindingAction.PreviousView => Create(ConsoleKey.LeftArrow, ConsoleModifiers.Alt),
            KeyBindingAction.MainView => Create(ConsoleKey.D1, ConsoleModifiers.Alt),
            KeyBindingAction.FileExplorerView => Create(ConsoleKey.D2, ConsoleModifiers.Alt),
            KeyBindingAction.OpenLogView => Create(ConsoleKey.D3, ConsoleModifiers.Alt),
            KeyBindingAction.HelpView => Create(ConsoleKey.H, ConsoleModifiers.Alt),
            KeyBindingAction.Cancel => Create(ConsoleKey.Escape, 0),
            KeyBindingAction.RefreshResults => Create(ConsoleKey.R, ConsoleModifiers.Alt),
            KeyBindingAction.OpenInExplorer => Create(ConsoleKey.O, ConsoleModifiers.Alt),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    internal static KeyChord Parse(KeyChordOptions options) =>
        new(
            Enum.Parse<ConsoleKey>(options.Key!, ignoreCase: true),
            options.Modifiers!.Equals("None", StringComparison.OrdinalIgnoreCase)
                ? 0
                : Enum.Parse<ConsoleModifiers>(options.Modifiers, ignoreCase: true)
        );

    private static KeyChordOptions Create(ConsoleKey key, ConsoleModifiers modifiers) =>
        new() { Key = key.ToString(), Modifiers = modifiers == 0 ? "None" : modifiers.ToString() };
}

public sealed class KeyBindingOptionsValidator : AbstractValidator<KeyBindingOptions>
{
    private static readonly HashSet<string> ActionNames = Enum.GetNames<KeyBindingAction>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> ChordPropertyNames = new(
        [nameof(KeyChordOptions.Key), nameof(KeyChordOptions.Modifiers)],
        StringComparer.OrdinalIgnoreCase
    );
    private static readonly HashSet<ConsoleKey> ReservedKeys =
    [
        ConsoleKey.Enter,
        ConsoleKey.Tab,
        ConsoleKey.Spacebar,
        ConsoleKey.Backspace,
        ConsoleKey.UpArrow,
        ConsoleKey.DownArrow,
        ConsoleKey.Home,
        ConsoleKey.End,
        ConsoleKey.PageUp,
        ConsoleKey.PageDown,
        ConsoleKey.Packet,
    ];

    public KeyBindingOptionsValidator(IConfiguration configuration)
    {
        RuleFor(x => x).Custom((options, context) => Validate(options, configuration, context));
    }

    private static void Validate(
        KeyBindingOptions options,
        IConfiguration configuration,
        ValidationContext<KeyBindingOptions> context
    )
    {
        var section = configuration.GetSection("KeyBindings");
        foreach (var actionSection in section.GetChildren())
        {
            if (!ActionNames.Contains(actionSection.Key))
            {
                context.AddFailure($"KeyBindings contains unknown action '{actionSection.Key}'.");
                continue;
            }

            foreach (var property in actionSection.GetChildren())
            {
                if (!ChordPropertyNames.Contains(property.Key))
                {
                    context.AddFailure(
                        $"KeyBindings:{actionSection.Key} contains unknown property '{property.Key}'."
                    );
                }
            }
        }

        var bindings = new Dictionary<KeyBindingAction, KeyChord>();
        foreach (var action in Enum.GetValues<KeyBindingAction>())
        {
            var chordOptions = GetOverride(options, action) ?? KeyBindingSet.GetDefault(action);
            if (!TryParse(action, chordOptions, context, out var chord))
            {
                continue;
            }
            bindings.Add(action, chord);
        }

        foreach (var (left, right) in GetPairs(bindings.Keys))
        {
            if (CanCoexist(left, right) && bindings[left].Key == bindings[right].Key)
            {
                context.AddFailure(
                    $"KeyBindings:{left} and KeyBindings:{right} overlap because both use {bindings[left].Key}."
                );
            }
        }
    }

    private static bool TryParse(
        KeyBindingAction action,
        KeyChordOptions options,
        ValidationContext<KeyBindingOptions> context,
        [NotNullWhen(true)] out KeyChord? chord
    )
    {
        chord = null;
        if (string.IsNullOrWhiteSpace(options.Key) || string.IsNullOrWhiteSpace(options.Modifiers))
        {
            context.AddFailure(
                $"KeyBindings:{action} must specify both Key and Modifiers. Use 'None' for no required modifiers."
            );
            return false;
        }

        if (
            !Enum.TryParse<ConsoleKey>(options.Key, ignoreCase: true, out var key)
            || !Enum.IsDefined(key)
        )
        {
            context.AddFailure($"KeyBindings:{action}:Key '{options.Key}' is not valid.");
            return false;
        }

        ConsoleModifiers modifiers = default;
        if (
            !options.Modifiers.Equals("None", StringComparison.OrdinalIgnoreCase)
            && !Enum.TryParse(options.Modifiers, ignoreCase: true, out modifiers)
        )
        {
            context.AddFailure(
                $"KeyBindings:{action}:Modifiers '{options.Modifiers}' is not valid."
            );
            return false;
        }

        const ConsoleModifiers allModifiers =
            ConsoleModifiers.Alt | ConsoleModifiers.Control | ConsoleModifiers.Shift;
        if ((modifiers & ~allModifiers) != 0)
        {
            context.AddFailure(
                $"KeyBindings:{action}:Modifiers contains an unsupported numeric value."
            );
            return false;
        }

        chord = new(key, modifiers);
        if (ReservedKeys.Contains(key))
        {
            context.AddFailure($"KeyBindings:{action} uses reserved prompt key '{key}'.");
        }
        if (key == ConsoleKey.A && modifiers.HasFlag(ConsoleModifiers.Alt))
        {
            context.AddFailure($"KeyBindings:{action} uses reserved prompt chord 'Alt + A'.");
        }
        if (
            IsPrintable(key)
            && !modifiers.HasFlag(ConsoleModifiers.Control)
            && !modifiers.HasFlag(ConsoleModifiers.Alt)
        )
        {
            context.AddFailure(
                $"KeyBindings:{action} must not intercept an unmodified or Shift-only printable key."
            );
        }

        return true;
    }

    private static KeyChordOptions? GetOverride(
        KeyBindingOptions options,
        KeyBindingAction action
    ) =>
        action switch
        {
            KeyBindingAction.NextView => options.NextView,
            KeyBindingAction.PreviousView => options.PreviousView,
            KeyBindingAction.MainView => options.MainView,
            KeyBindingAction.FileExplorerView => options.FileExplorerView,
            KeyBindingAction.OpenLogView => options.OpenLogView,
            KeyBindingAction.HelpView => options.HelpView,
            KeyBindingAction.Cancel => options.Cancel,
            KeyBindingAction.RefreshResults => options.RefreshResults,
            KeyBindingAction.OpenInExplorer => options.OpenInExplorer,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    private static IEnumerable<(KeyBindingAction Left, KeyBindingAction Right)> GetPairs(
        IEnumerable<KeyBindingAction> actions
    )
    {
        var values = actions.ToArray();
        for (var left = 0; left < values.Length; left++)
        {
            for (var right = left + 1; right < values.Length; right++)
            {
                yield return (values[left], values[right]);
            }
        }
    }

    private static bool CanCoexist(KeyBindingAction left, KeyBindingAction right) =>
        IsNavigation(left)
        || IsNavigation(right)
        || (left == KeyBindingAction.Cancel && right == KeyBindingAction.OpenInExplorer)
        || (left == KeyBindingAction.OpenInExplorer && right == KeyBindingAction.Cancel);

    private static bool IsNavigation(KeyBindingAction action) =>
        action
            is KeyBindingAction.NextView
                or KeyBindingAction.PreviousView
                or KeyBindingAction.MainView
                or KeyBindingAction.FileExplorerView
                or KeyBindingAction.OpenLogView
                or KeyBindingAction.HelpView;

    private static bool IsPrintable(ConsoleKey key) =>
        key is >= ConsoleKey.A and <= ConsoleKey.Z
        || key is >= ConsoleKey.D0 and <= ConsoleKey.D9
        || key is >= ConsoleKey.NumPad0 and <= ConsoleKey.NumPad9
        || key
            is ConsoleKey.Add
                or ConsoleKey.Decimal
                or ConsoleKey.Divide
                or ConsoleKey.Multiply
                or ConsoleKey.Oem1
                or ConsoleKey.Oem2
                or ConsoleKey.Oem3
                or ConsoleKey.Oem4
                or ConsoleKey.Oem5
                or ConsoleKey.Oem6
                or ConsoleKey.Oem7
                or ConsoleKey.Oem8
                or ConsoleKey.OemComma
                or ConsoleKey.OemMinus
                or ConsoleKey.OemPeriod
                or ConsoleKey.OemPlus
                or ConsoleKey.Subtract;
}
