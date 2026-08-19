namespace Domain.Common;

public sealed record ValidationError(string Field, string Message);
