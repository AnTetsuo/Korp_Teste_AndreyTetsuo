namespace Domain.Common;

public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception(
        "A concurrent write changed the same row. Reload the entity and run the use case again.",
        innerException);
