using Serara.Console;
using Serara.Core.Configuration;
using Serara.Core.Search;

namespace Serara.Hosting;

internal sealed class HostingCompositionValidator : IModuleConfigurationValidator
{
    private readonly IServiceScopeFactory _scopeFactory;

    public HostingCompositionValidator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Name => "Hosting";

    public IReadOnlyList<string> Validate(IServiceProvider serviceProvider)
    {
        using var scope = _scopeFactory.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var workflows = scopedProvider.GetServices<ISearchWorkflow>().ToList();
        var actions = scopedProvider.GetServices<IInteractiveAction>().ToList();
        var backends = serviceProvider.GetServices<ISearchBackend>().ToList();

        List<string> errors = [];
        if (workflows.Count == 0)
        {
            errors.Add("At least one search workflow must be registered.");
        }

        AddDuplicateIdErrors(workflows.Select(workflow => workflow.Id), "search workflow", errors);
        AddDuplicateIdErrors(actions.Select(action => action.Id), "interactive action", errors);

        foreach (var workflow in workflows)
        {
            var matchingBackends = backends
                .Where(backend => backend.RequestType.IsAssignableFrom(workflow.RequestType))
                .ToList();
            if (matchingBackends.Count == 0)
            {
                errors.Add(
                    $"Search workflow '{workflow.Id}' has no backend for request type '{workflow.RequestType}'."
                );
            }
            else if (matchingBackends.Count > 1)
            {
                errors.Add(
                    $"Search workflow '{workflow.Id}' has multiple backends for request type "
                        + $"'{workflow.RequestType}': "
                        + $"{string.Join(", ", matchingBackends.Select(backend => backend.GetType()))}."
                );
            }
        }

        return errors;
    }

    private static void AddDuplicateIdErrors(
        IEnumerable<string> ids,
        string capabilityName,
        List<string> errors
    )
    {
        foreach (
            var id in ids.GroupBy(id => id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
        )
        {
            errors.Add($"Duplicate {capabilityName} ID '{id}'.");
        }
    }
}
