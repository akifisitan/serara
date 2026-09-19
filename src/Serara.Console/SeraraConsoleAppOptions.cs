using FluentValidation;

namespace Serara.Console;

public sealed record SeraraConsoleAppOptions
{
    public int NumConcurrentSearchOperations { get; set; } = 1;
    public bool VerboseLogging { get; set; }
    public int MaxSearchResults { get; set; } = 10_000;
}

public sealed class SeraraConsoleAppOptionsValidator : AbstractValidator<SeraraConsoleAppOptions>
{
    public SeraraConsoleAppOptionsValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.NumConcurrentSearchOperations).InclusiveBetween(1, 10);
        RuleFor(x => x.MaxSearchResults).InclusiveBetween(1, 100_000);
    }
}
