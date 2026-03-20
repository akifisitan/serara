using FluentValidation;

namespace Serara.Files;

public sealed class FluentValidationOptions<T> : IValidateOptions<T>
    where T : class
{
    private readonly IValidator<T> _validator;

    public FluentValidationOptions(IValidator<T> validator)
    {
        _validator = validator;
    }

    // Will only use unnamed options so leaving name alone
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
