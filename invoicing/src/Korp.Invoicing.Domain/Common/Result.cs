namespace Domain.Common;

public class Result
{
    private static readonly IReadOnlyList<ValidationError> NoValidationErrors = [];

    protected Result(
        ResultStatus status,
        string? errorMessage = null,
        IReadOnlyList<ValidationError>? validationErrors = null)
    {
        Status = status;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? NoValidationErrors;
    }

    public ResultStatus Status { get; }

    public string? ErrorMessage { get; }

    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public bool IsSuccess => Status is ResultStatus.Ok or ResultStatus.Created;

    public static Result Success() => new(ResultStatus.Ok);

    public static Result Invalid(params ValidationError[] errors) =>
        new(ResultStatus.Invalid, "One or more validation errors occurred.", errors);

    public static Result Invalid(string field, string message) =>
        Invalid(new ValidationError(field, message));

    public static Result NotFound(string? message = null) =>
        new(ResultStatus.NotFound, message);

    public static Result Conflict(string? message = null) =>
        new(ResultStatus.Conflict, message);

    public static Result Unauthorized(string? message = null) =>
        new(ResultStatus.Unauthorized, message);

    public static Result Forbidden(string? message = null) =>
        new(ResultStatus.Forbidden, message);

    public static Result Error(string message) =>
        new(ResultStatus.Error, message);
}

public sealed class Result<T> : Result
{
    private Result(T value, ResultStatus status) : base(status) => Value = value;

    private Result(
        ResultStatus status,
        string? errorMessage = null,
        IReadOnlyList<ValidationError>? validationErrors = null)
        : base(status, errorMessage, validationErrors)
    {
    }

    public T Value => IsSuccess
        ? field!
        : throw new InvalidOperationException(
            $"Cannot access {nameof(Value)} of a failed result (status: {Status}).");

    public static Result<T> Success(T value) => new(value, ResultStatus.Ok);

    public static Result<T> Created(T value) => new(value, ResultStatus.Created);

    public new static Result<T> Invalid(params ValidationError[] errors) =>
        new(ResultStatus.Invalid, "One or more validation errors occurred.", errors);

    public new static Result<T> Invalid(string field, string message) =>
        Invalid(new ValidationError(field, message));

    public new static Result<T> NotFound(string? message = null) =>
        new(ResultStatus.NotFound, message);

    public new static Result<T> Conflict(string? message = null) =>
        new(ResultStatus.Conflict, message);

    public new static Result<T> Unauthorized(string? message = null) =>
        new(ResultStatus.Unauthorized, message);

    public new static Result<T> Forbidden(string? message = null) =>
        new(ResultStatus.Forbidden, message);

    public new static Result<T> Error(string message) =>
        new(ResultStatus.Error, message);

    public static implicit operator Result<T>(T value) => Success(value);
}
