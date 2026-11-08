namespace Serara.Search;

public sealed record SearchExecutionOptions
{
    public bool StopSearchAfterFirstMatch { get; set; }
    public int NumConcurrentSearchOperations { get; set; } = 4;
    public int QueueCapacityMultiplier { get; set; } = 2;
}
