namespace Ambio.Application.Abstractions.OperationResult;

public enum ResultKind
{
    Undefined = 0,
    NotFound,
    ValidationFailed,
    Conflict,
}
