using Ambio.Application.JobApplications;

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
        JobApplicationServices jobApplicationServices)
    {
        var jobApplications =  await jobApplicationServices.GetAll();

        return TypedResults.Ok(jobApplications);
    }

    private static async Task<IResult> GetHandler(
        Guid id,
        JobApplicationServices jobApplicationServices)
    {
        var jobApplication = await jobApplicationServices.GetById(id);

        return jobApplication is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(jobApplication);
    }

    private static async Task<Results<ValidationProblem, Created<JobApplicationResponse>>> CreateHandler (
        JobApplicationRequest jobApplicationRequest,
        JobApplicationServices jobApplicationServices)
    {
        var validationResult = await jobApplicationServices.Validate(jobApplicationRequest);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var jobApplication = await jobApplicationServices.Create(jobApplicationRequest);

        return TypedResults.Created($"/job-applications/{jobApplication.Id}", jobApplication);
    }

    private static async Task<Results<NotFound, ValidationProblem, Ok<JobApplicationResponse>>> UpdateHandler (
        Guid id,
        JobApplicationRequest jobApplicationRequest,
        JobApplicationServices jobApplicationServices)
    {
        var validationResult = await jobApplicationServices.Validate(jobApplicationRequest);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var existingApplication = await jobApplicationServices.Update(id, jobApplicationRequest);

        return existingApplication is null
            ? (Results<NotFound, ValidationProblem, Ok<JobApplicationResponse>>)TypedResults.NotFound()
            : (Results<NotFound, ValidationProblem, Ok<JobApplicationResponse>>)TypedResults.Ok(
                existingApplication);
    }

    private static async Task<Results<NotFound, NoContent>> DeleteHandler (Guid id, JobApplicationServices jobApplicationServices)
    {
        var isApplicationDeleted = await jobApplicationServices.Delete(id);

        return isApplicationDeleted
            ? (Results<NotFound, NoContent>)TypedResults.NoContent()
            : (Results<NotFound, NoContent>)TypedResults.NotFound();
    }
}
