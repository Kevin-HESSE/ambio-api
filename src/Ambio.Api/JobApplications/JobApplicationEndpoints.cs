using Ambio.Application.JobApplications;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Ambio.Api.JobApplications;

public static class JobApplicationEndpoints
{
    private const string Prefix = "/job-applications";

    public static void MapJobApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var baseEndpoint = app.MapGroup(Prefix);

        baseEndpoint.MapGet("/", GetCollectionHandler);
        baseEndpoint.MapGet("/{id:guid}", GetHandler);
        baseEndpoint.MapPost("/", CreateHandler);
        baseEndpoint.MapPut("/{id:guid}", UpdateHandler);
        baseEndpoint.MapDelete("/{id:guid}", DeleteHandler);
    }

    private static async Task<IResult> GetCollectionHandler(
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var jobApplications = await jobApplicationService.GetAll(cancellationToken);

        return TypedResults.Ok(jobApplications);
    }

    private static async Task<Results<NotFound, Ok<JobApplicationResponse>>> GetHandler(
        Guid id,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var jobApplication = await jobApplicationService.GetById(id, cancellationToken);

        return jobApplication is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(jobApplication);
    }

    private static async Task<Results<ValidationProblem, Created<JobApplicationResponse>>> CreateHandler(
        JobApplicationRequest jobApplicationRequest,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var jobApplication = await jobApplicationService.Create(jobApplicationRequest, cancellationToken);

            return TypedResults.Created($"{Prefix}/{jobApplication.Id}", jobApplication);
        }
        catch (ValidationException ex)
        {
            return TypedResults.ValidationProblem(new ValidationResult(ex.Errors).ToDictionary());
        }
    }

    private static async Task<Results<NotFound, ValidationProblem, Ok<JobApplicationResponse>>> UpdateHandler(
        Guid id,
        JobApplicationRequest jobApplicationRequest,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingApplication = await jobApplicationService.Update(id, jobApplicationRequest, cancellationToken);

            return existingApplication is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(existingApplication);
        }
        catch (ValidationException ex)
        {
            return TypedResults.ValidationProblem(new ValidationResult(ex.Errors).ToDictionary());
        }
    }

    private static async Task<Results<NotFound, NoContent>> DeleteHandler(
        Guid id,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var isApplicationDeleted = await jobApplicationService.Delete(id, cancellationToken);

        return isApplicationDeleted
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
