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

        public static ResultFor<T> NotArchived()
        {
            var errors = OperationError.Conflict(
                code: "NOT_ARCHIVED",
                description: "The resource is not archived.");

            return ResultFor<T>.Failure(ResultKind.Conflict, errors);
        }

        public static ResultFor<T> StatusIdentical()
        {
            var errors = OperationError.Conflict(
                code: "STATUS_IDENTICAL",
                description: "The new status is identical to the current one.");

            return ResultFor<T>.Failure(ResultKind.Conflict, errors);
        }

        public static ResultFor<T> StatusNotFollowingWorkflow()
        {
            var errors = OperationError.Conflict(
                code: "INVALID_WORKFLOW",
                description: "The new status is not following the workflow.");

            return ResultFor<T>.Failure(ResultKind.Conflict, errors);
        }

        public static ResultFor<T> DateBeforePreviousState()
        {
            var errors = OperationError.Validation(
                code: "DATE_BEFORE_PREVIOUS_STATE",
                description: "The date provided is set before the previous state.");

            return ResultFor<T>.Failure(ResultKind.ValidationFailed, errors);
        }
    }
}
