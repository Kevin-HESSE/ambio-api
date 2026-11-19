namespace Ambio.Application.Abstractions.OperationResult;

public readonly record struct OperationError
{
    public string Code { get; }
    public string Description { get; }

    private OperationError(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public static OperationError Validation(
        string code = "Validation",
        string description = "A validation error occurred"
        ) => new(code, description);

    public static OperationError Unexpected(
        string code = "Unexpected",
        string description = "An unexpected error occurred"
    ) => new(code, description);

    public static OperationError Conflict(
        string code = "Conflict",
        string description = "An unexpected conflict occurred"
    ) => new(code, description);
}
