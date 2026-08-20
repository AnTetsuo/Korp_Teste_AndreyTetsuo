namespace Domain.Common;

public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception(
        "A concurrent write changed the same rows. Reload the entities and run the use case again.",
        innerException);
