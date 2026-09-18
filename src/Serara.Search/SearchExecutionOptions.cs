namespace Serara.Search;

public sealed class SearchExecutionOptions
{
    public int NumConcurrentSearchOperations { get; set; } = 4;
    public int QueueCapacityMultiplier { get; set; } = 2;
}
