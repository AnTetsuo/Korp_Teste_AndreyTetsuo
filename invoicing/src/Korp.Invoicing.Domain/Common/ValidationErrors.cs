namespace Domain.Common;

public sealed class ValidationErrors
{
    private readonly List<ValidationError> _errors = [];

    public bool Any => _errors.Count > 0;

    public ValidationErrors Add(string field, string message)
    {
        _errors.Add(new ValidationError(field, message));

        return this;
    }

    public ValidationErrors Require(bool satisfied, string field, string message) =>
        satisfied ? this : Add(field, message);

    public ValidationErrors RequireText(string? value, string field, string label, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? Add(field, $"{label} is required.")
            : value.Trim().Length > maxLength
                ? Add(field, $"{label} must be at most {maxLength} characters.")
                : this;

    public ValidationErrors RequireId(Guid value, string field, string label) =>
        Require(value != Guid.Empty, field, $"{label} is required.");

    public ValidationErrors RequirePositive(int value, string field, string label) =>
        Require(value > 0, field, $"{label} must be greater than zero.");

    public ValidationError[] ToArray() => [.. _errors];
}
