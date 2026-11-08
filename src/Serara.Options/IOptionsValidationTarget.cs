namespace Serara.Options;

public interface IOptionsValidationTarget
{
    OptionsValidationFailure? Validate();
}
