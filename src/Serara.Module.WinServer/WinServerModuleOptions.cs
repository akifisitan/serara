using FluentValidation;

namespace Serara.Module.WinServer;

public sealed record WinServerModuleOptions
{
    public required string ArchiveServerBasePath { get; set; }
    public required string LiveServerBasePath { get; set; }
    public required string LogPathPrefix { get; set; }
}

public sealed class WinServerModuleOptionsValidator : AbstractValidator<WinServerModuleOptions>
{
    public WinServerModuleOptionsValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ArchiveServerBasePath)
            .Must(Directory.Exists)
            .WithMessage(x => $"Directory {x.ArchiveServerBasePath} does not exist")
            .When(x => !string.IsNullOrWhiteSpace(x.ArchiveServerBasePath));

        RuleFor(x => x.LiveServerBasePath).NotEmpty().WithMessage("Must not be empty");

        RuleFor(x => x.LogPathPrefix).MaximumLength(100);
    }
}

public sealed record WinServerSearchOptions
{
    public int SearchDelayMinutes { get; set; } = 5;
    public int MinimumSearchPatternLength { get; set; } = 6;
}

public sealed class WinServerSearchOptionsValidator : AbstractValidator<WinServerSearchOptions>
{
    public WinServerSearchOptionsValidator()
    {
        RuleFor(x => x.SearchDelayMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumSearchPatternLength).GreaterThan(0);
    }
}
