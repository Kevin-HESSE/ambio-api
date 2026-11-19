using FluentValidation.Results;

namespace Ambio.Application.Abstractions.OperationResult;

public static class ResultForExtensions
{
    extension<T>(ResultFor<T>)
    {
        public static ResultFor<T> NotFound()
        {
            var error = OperationError.Unexpected(
                code: "NOT_FOUND",
                description: "The resource was not found.");

            return ResultFor<T>.Failure(ResultKind.NotFound, error);
        }

        public static ResultFor<T> NotValid(List<ValidationFailure> validationErrors)
        {
            var errors = validationErrors.ConvertAll(e =>
                OperationError.Validation(
                    code: e.PropertyName,
                    description: e.ErrorMessage));

            return ResultFor<T>.Failure(ResultKind.ValidationFailed, errors);
        }

        public static ResultFor<T> AlreadyArchived()
        {
            var errors = OperationError.Conflict(
                code: "ALREADY_ARCHIVED",
                description: "The resource is already archived.");

            return ResultFor<T>.Failure(ResultKind.Conflict, errors);
        }
    }
}
