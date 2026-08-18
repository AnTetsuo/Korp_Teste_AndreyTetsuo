using System.Text.Json;
using Domain.Common;
using FluentValidation;

namespace Api.Extensions;

internal sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
            return Result.Invalid("body", "A request body is required.").ToProblem();

        var validation = await validator.ValidateAsync(
            request, context.HttpContext.RequestAborted);

        if (validation.IsValid)
            return await next(context);

        var errors = validation.Errors
            .Select(failure => new ValidationError(
                JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName),
                failure.ErrorMessage))
            .ToArray();

        return Result.Invalid(errors).ToProblem();
    }
}
