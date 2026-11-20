using Ambio.Application.Abstractions.OperationResult;

namespace Ambio.Api.Abstractions;

public class HttpResultFor<T>
{
    private readonly T _value;
    private IResult? _result;

    internal HttpResultFor(T value)
    {
        _value = value;
    }

    public HttpResultFor<T> When(Func<T, bool> condition, Func<T, IResult> factory)
    {
        if (_result is null && condition(_value))
        {
            _result = factory(_value);
        }

        return this;
    }

    public IResult Otherwise(Func<T, IResult> fallback) => _result ?? fallback(_value);
}

public static class HttpResultFactory
{
    public static HttpResultFor<ResultFor<T>> ConvertToHttpResult<T>(this ResultFor<T> value) => new(value);

    public static HttpResultFor<ResultFor<T>> IsUpdated<T>(this HttpResultFor<ResultFor<T>> result)
        => result.When(r => r.IsSuccess, r => TypedResults.Ok(r.Value));

    public static HttpResultFor<ResultFor<T>> IsASuccess<T>(this HttpResultFor<ResultFor<T>> result,
        Func<T, IResult> onSuccess)
        => result.When(r => r.IsSuccess, r => onSuccess(r.Value));

    public static HttpResultFor<ResultFor<T>> OrHaveInvalidData<T>(this HttpResultFor<ResultFor<T>> result)
        => result.When(
            condition: r => r.Kind == ResultKind.ValidationFailed,
            factory: r => TypedResults.ValidationProblem(r.ToDictionary()));

    public static HttpResultFor<ResultFor<T>> OrAlreadyArchived<T>(this HttpResultFor<ResultFor<T>> result)
        => result.When(
            condition: r => r.Kind == ResultKind.Conflict,
            factory: r => TypedResults.Conflict());

    public static IResult OrFinallyNotFound<T>(this HttpResultFor<ResultFor<T>> result)
        => result.Otherwise(_ => TypedResults.NotFound());
}
