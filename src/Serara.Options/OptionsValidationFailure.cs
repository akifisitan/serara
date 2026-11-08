namespace Serara.Options;

public sealed record OptionsValidationFailure(
    Type OptionsType,
    string? OptionsName,
    IReadOnlyList<string> Failures
);
