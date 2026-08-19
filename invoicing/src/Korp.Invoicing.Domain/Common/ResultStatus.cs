namespace Domain.Common;

public enum ResultStatus
{
    Ok = 0,
    Created = 1,
    Invalid = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6,
    Error = 7
}
