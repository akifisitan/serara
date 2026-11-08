namespace Serara.Search;

public sealed record SearchExecutionOptions
{
    public bool StopSearchAfterFirstMatch { get; set; }
    public int NumConcurrentSearchOperations { get; set; }
    public int QueueCapacityMultiplier { get; set; }
}
