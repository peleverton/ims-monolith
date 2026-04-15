using FluentValidation;
using IMS.Modular.Shared.Domain;
using MediatR;

namespace IMS.Modular.Shared.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior that auto-validates incoming requests using FluentValidation.
/// Returns Result.Failure with validation errors instead of throwing exceptions.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Build a structured error message with all validation failures
        var errors = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));

        // If the response is Result<T>, return a typed failure
        var responseType = typeof(TResponse);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(nameof(Result<object>.Failure),
                new[] { typeof(string), typeof(int) });

            if (failureMethod is not null)
            {
                return (TResponse)failureMethod.Invoke(null, new object[] { errors, 422 })!;
            }
        }

        // If it's a non-generic Result
        if (responseType == typeof(Result))
        {
            var result = Result.Failure(errors, 422);
            return (TResponse)(object)result;
        }

        // Fallback: throw for non-Result responses
        throw new ValidationException(failures);
    }
}
