using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Serara.Options;

public sealed class FluentValidationOptionsValidator<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T
> : IValidateOptions<T>
    where T : class
{
    private readonly IValidator<T> _validator;

    public FluentValidationOptionsValidator(IValidator<T> validator)
    {
        _validator = validator;
    }

    public ValidateOptionsResult Validate(string? name, T options)
    {
        var result = _validator.Validate(options);

        if (result.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = result.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}").ToArray();

        return ValidateOptionsResult.Fail(failures);
    }
}
