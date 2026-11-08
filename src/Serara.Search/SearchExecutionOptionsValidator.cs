using FluentValidation;

namespace Serara.Search;

public sealed class SearchExecutionOptionsValidator : AbstractValidator<SearchExecutionOptions>
{
    public SearchExecutionOptionsValidator()
    {
        RuleFor(x => x.NumConcurrentSearchOperations).InclusiveBetween(1, 10);
        RuleFor(x => x.QueueCapacityMultiplier).InclusiveBetween(2, 4);
    }
}
