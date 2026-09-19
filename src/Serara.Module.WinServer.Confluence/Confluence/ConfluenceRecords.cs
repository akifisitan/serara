using FluentValidation;

namespace Serara.Module.WinServer;

public sealed record ConfluenceMetadataLoaderOptions
{
    public required long PageId { get; set; }
    public string UK { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string ACI { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string Servers { get; set; } = string.Empty;
    public string LogPath { get; set; } = string.Empty;
    public string ArchiveDirectoryName { get; set; } = string.Empty;
    public string SearchTerm { get; set; } = string.Empty;
    public string LogSearchPattern { get; set; } = string.Empty;
}

public sealed class ConfluenceMetadataLoaderOptionsValidator
    : AbstractValidator<ConfluenceMetadataLoaderOptions>
{
    public ConfluenceMetadataLoaderOptionsValidator()
    {
        RuleFor(x => x.PageId).Must(x => x > 0).WithMessage("Must be bigger than 0");
        RuleFor(x => x.UK).NotEmpty();
        RuleFor(x => x.TeamName).NotEmpty();
        RuleFor(x => x.ACI).NotEmpty();
        RuleFor(x => x.ApplicationName).NotEmpty();
        RuleFor(x => x.Servers).NotEmpty();
        RuleFor(x => x.LogPath).NotEmpty();
        RuleFor(x => x.ArchiveDirectoryName).NotEmpty();
    }
}
