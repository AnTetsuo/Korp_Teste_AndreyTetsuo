namespace Domain.Common;

public sealed class UniqueConstraintViolationException(string? constraintName, Exception innerException)
    : Exception($"A unique constraint ({constraintName ?? "unnamed"}) was violated.", innerException)
{
    public string? ConstraintName { get; } = constraintName;
}
