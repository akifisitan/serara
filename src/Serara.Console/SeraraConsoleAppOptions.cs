using FluentValidation;

namespace Serara.Console;

public sealed record SeraraConsoleAppOptions
{
    public bool VerboseLogging { get; set; }
    public int MaxSearchResults { get; set; } = 10_000;
}

public sealed class SeraraConsoleAppOptionsValidator : AbstractValidator<SeraraConsoleAppOptions>
{
    public SeraraConsoleAppOptionsValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.MaxSearchResults).InclusiveBetween(1, 100_000);
    }
}
