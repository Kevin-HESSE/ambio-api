using System.Diagnostics.CodeAnalysis;

namespace Ambio.Application.Abstractions.OperationResult;

public class ResultFor<T>
{
    private readonly T? _value;
    private readonly List<OperationError>? _errors;

    private ResultFor(
        T? value = default,
        ResultKind kind = ResultKind.Undefined,
        List<OperationError>? errors = null)
    {
        _value = value;
        _errors = errors;
        Kind = kind;
    }

    [MemberNotNullWhen(false, nameof(_errors))]
    [MemberNotNullWhen(false, nameof(Errors))]
    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => _errors is null;

    [MemberNotNullWhen(true, nameof(_errors))]
    [MemberNotNullWhen(true, nameof(Errors))]
    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => !IsSuccess;

    public T Value => _value!;

    public ResultKind Kind { get; }

    public List<OperationError> Errors => _errors!;

    public Dictionary<string, string[]> ToDictionary() =>
        Errors
            .GroupBy(e => e.Code)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray());

    public static ResultFor<T> Success(T value) => new(value);
    public static ResultFor<T> Failure(ResultKind failureKind, OperationError error) =>
        new(kind: failureKind, errors: [error]);
    public static ResultFor<T> Failure(ResultKind failureKind, List<OperationError> errors) =>
        new(kind: failureKind, errors: errors);
}
