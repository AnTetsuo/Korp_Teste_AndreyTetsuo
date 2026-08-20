using Domain.Common;

namespace Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.ToProblem();

    public static IResult ToProblem(this Result result) => result.Status switch
    {
        ResultStatus.Invalid => Results.ValidationProblem(FieldKeyedErrors(result)),

        ResultStatus.NotFound => Results.Problem(
            detail: result.ErrorMessage ?? "Resource not found.",
            statusCode: StatusCodes.Status404NotFound),

        ResultStatus.Conflict => Results.Problem(
            detail: result.ErrorMessage ?? "The request conflicts with the current state.",
            statusCode: StatusCodes.Status409Conflict,
            extensions: result.ValidationErrors.Count == 0 ? null : new Dictionary<string, object?>
            {
                ["errors"] = FieldKeyedErrors(result)
            }),

        ResultStatus.Unauthorized => Results.Problem(statusCode: StatusCodes.Status401Unauthorized),

        ResultStatus.Forbidden => Results.Problem(statusCode: StatusCodes.Status403Forbidden),

        _ => Results.Problem(
            detail: result.ErrorMessage ?? "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError)
    };

    private static Dictionary<string, string[]> FieldKeyedErrors(Result result) =>
        result.ValidationErrors
            .GroupBy(e => e.Field)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
}
