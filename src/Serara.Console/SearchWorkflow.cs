using Serara.Core.Search;

namespace Serara.Console;

public sealed record SearchWorkflowSelection(ISearchRequest Request, string Display);

public sealed record SearchWorkflowSelection<TRequest>(TRequest Request, string Display)
    where TRequest : ISearchRequest;

public interface ISearchWorkflow
{
    string Id { get; }
    string DisplayName { get; }
    Type RequestType { get; }
    Task<SearchWorkflowSelection> RetrieveSearch(CancellationToken cancellationToken);
}

public abstract class SearchWorkflow<TRequest> : ISearchWorkflow
    where TRequest : ISearchRequest
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public Type RequestType => typeof(TRequest);

    async Task<SearchWorkflowSelection> ISearchWorkflow.RetrieveSearch(
        CancellationToken cancellationToken
    )
    {
        var selection = await RetrieveSearch(cancellationToken).ConfigureAwait(false);
        return new SearchWorkflowSelection(selection.Request, selection.Display);
    }

    protected abstract Task<SearchWorkflowSelection<TRequest>> RetrieveSearch(
        CancellationToken cancellationToken
    );
}
