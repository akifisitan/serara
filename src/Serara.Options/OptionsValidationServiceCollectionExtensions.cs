using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Serara.Options;

public static class OptionsValidationServiceCollectionExtensions
{
    public static OptionsBuilder<T> WithValidator<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            T,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValidator
    >(this OptionsBuilder<T> builder)
        where T : class
        where TValidator : class, IValidator<T>
    {
        var services = builder.Services;
        services.AddSingleton<IValidator<T>, TValidator>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<T>, FluentValidationOptionsValidator<T>>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOptionsValidationTarget, OptionsValidationTarget<T>>()
        );

        return builder;
    }
}
