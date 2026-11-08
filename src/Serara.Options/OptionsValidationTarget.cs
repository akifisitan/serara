using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Serara.Options;

public sealed class OptionsValidationTarget<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T
> : IOptionsValidationTarget
    where T : class
{
    private readonly IOptions<T> _options;

    public OptionsValidationTarget(IOptions<T> options)
    {
        _options = options;
    }

    public OptionsValidationFailure? Validate()
    {
        try
        {
            _ = _options.Value;
            return null;
        }
        catch (OptionsValidationException ex)
        {
            return new OptionsValidationFailure(
                ex.OptionsType,
                ex.OptionsName,
                ex.Failures.ToList()
            );
        }
    }
}
