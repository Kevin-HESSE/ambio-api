using Ambio.Application.Abstractions.OperationResult;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Ambio.Api.Abstractions;

public static class ResultForHttpExtensions
{
    extension<T>(ResultFor<T> result)
    {
        public Results<TSuccess, TError> Match<TSuccess, TError>(
            Func<T, TSuccess> onSuccess,
            Func<Dictionary<string, string[]>, TError> onError)
        where TSuccess : IResult
        where TError : IResult
        {
            return result.IsSuccess
                ? onSuccess(result.Value)
                : onError(result.ToDictionary());
        }

        public Results<TSuccess, TInvalid, TError> Match<TSuccess, TInvalid, TError>(
            Func<T, TSuccess> onSuccess,
            Func<Dictionary<string, string[]>, TInvalid> onInvalid,
            Func<Dictionary<string, string[]>, TError> onError)
            where TSuccess : IResult
            where TInvalid : IResult
            where TError : IResult
        {
            if (result.IsSuccess)
            {
                return onSuccess(result.Value);
            }

            return result.Kind switch
            {
                ResultKind.ValidationFailed or ResultKind.Conflict => onInvalid(result.ToDictionary()),
                _ => onError(result.ToDictionary())
            };
        }

        public Results<TSuccess, TInvalid, TArchivedError, TError> Match<TSuccess, TInvalid, TArchivedError, TError>(
            Func<T, TSuccess> onSuccess,
            Func<Dictionary<string, string[]>, TInvalid> onInvalid,
            Func<Dictionary<string, string[]>, TArchivedError> onArchived,
            Func<Dictionary<string, string[]>, TError> onError)
            where TSuccess : IResult
            where TInvalid : IResult
            where TArchivedError : IResult
            where TError : IResult
        {
            if (result.IsSuccess)
            {
                return onSuccess(result.Value);
            }

            return result.Kind switch
            {
                ResultKind.ValidationFailed => onInvalid(result.ToDictionary()),
                ResultKind.Conflict => onArchived(result.ToDictionary()),
                _ => onError(result.ToDictionary())
            };
        }
    }
}
