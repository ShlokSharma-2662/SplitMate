using FluentValidation;
using MediatR;

namespace SplitMate.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all FluentValidation validators for a request
/// and short-circuits with a failed <see cref="Result"/> when validation fails.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result, new()
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var errors = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => f.ErrorMessage)
            .Distinct()
            .ToList();

        return errors.Count > 0
            ? Result.Failure<TResponse>(errors)
            : await next();
    }
}
