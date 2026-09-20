namespace Serara.Search;

public sealed record SearchExecutionOptions
{
    public int NumConcurrentSearchOperations { get; set; }
    public int QueueCapacityMultiplier { get; set; }
}
