using Ambio.Api.Abstractions;
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

    private static async Task<Results<Ok<PaginationResponse<JobApplicationResponse>>, ValidationProblem>>
        GetCollectionHandler(
            [AsParameters] JobApplicationQuery filter,
            HttpRequest httpRequest,
            JobApplicationService jobApplicationService,
            CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.GetAll(filter, cancellationToken);

        return result.Match(
            onSuccess: (collectionResponse) =>
            {
                var meta = PaginationMetaFactory.Create(
                    request: httpRequest,
                    currentPage: collectionResponse.CurrentPage,
                    lastPage: collectionResponse.MaxPage,
                    itemsPerPage: collectionResponse.ItemsPerPage,
                    totalItems: collectionResponse.Total);

                var response = new PaginationResponse<JobApplicationResponse>(collectionResponse.Items, meta);

                return TypedResults.Ok(response);
            },
            onError: errors => TypedResults.ValidationProblem(errors));
    }

    private static async Task<Results<Ok<JobApplicationResponse>, NotFound>> GetHandler(
        Guid id,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.GetById(id, cancellationToken);

        return result.Match(
            onSuccess: jobApplication => TypedResults.Ok(jobApplication),
            onError: _ => TypedResults.NotFound()
        );
    }

    private static async Task<Results<Created<JobApplicationResponse>, ValidationProblem>> CreateHandler(
        JobApplicationRequest jobApplicationRequest,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.Create(jobApplicationRequest, cancellationToken);

        return result.Match(
            onSuccess: jobApplication => TypedResults.Created($"{Prefix}/{jobApplication.Id}", jobApplication),
            onError: errors => TypedResults.ValidationProblem(errors)
        );
    }

    private static async Task<Results<Ok<JobApplicationResponse>, ValidationProblem, NotFound>> UpdateHandler(
        Guid id,
        JobApplicationRequest jobApplicationRequest,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.Update(id, jobApplicationRequest, cancellationToken);

        return result.Match(
            onSuccess: jobApplication => TypedResults.Ok(jobApplication),
            onInvalid: errors => TypedResults.ValidationProblem(errors),
            onError: _ => TypedResults.NotFound()
        );
    }

    private static async Task<Results<NoContent, NotFound>> DeleteHandler(
        Guid id,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.Delete(id, cancellationToken);

        return result.Match(
            onSuccess: _ => TypedResults.NoContent(),
            onError: _ => TypedResults.NotFound()
        );
    }
}
