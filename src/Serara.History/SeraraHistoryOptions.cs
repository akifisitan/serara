namespace Serara.History;

public sealed class SeraraHistoryOptions
{
    public bool Enabled { get; set; } = true;
    public string FilePath { get; set; } = "search-history.json";
}
