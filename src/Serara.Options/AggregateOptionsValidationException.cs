namespace Serara.Options;

public sealed class AggregateOptionsValidationException : Exception
{
    public IReadOnlyList<OptionsValidationFailure> Failures { get; }

    public AggregateOptionsValidationException(IReadOnlyList<OptionsValidationFailure> failures)
        : base(CreateMessage(failures))
    {
        Failures = failures;
    }

    private static string CreateMessage(IReadOnlyList<OptionsValidationFailure> failures)
    {
        var messages = failures.SelectMany(failure =>
        {
            var name = string.IsNullOrEmpty(failure.OptionsName)
                ? failure.OptionsType.Name
                : $"{failure.OptionsType.Name} ({failure.OptionsName})";

            return failure.Failures.Select(message => $"{name}: {message}");
        });

        return $"Configuration validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, messages)}";
    }
}
