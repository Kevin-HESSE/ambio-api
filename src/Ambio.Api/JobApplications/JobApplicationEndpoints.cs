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
        baseEndpoint.MapPost("/spontaneous", CreateSpontaneousHandler);
        baseEndpoint.MapPut("/{id:guid}", UpdateHandler);
        baseEndpoint.MapPost("/{id:guid}/status", UpdateStatusHandler);
        baseEndpoint.MapPost("/{id:guid}/archive", ArchiveHandler);
        baseEndpoint.MapPost("/{id:guid}/reopen", ReopenHandler);
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

    private static async Task<Results<Ok<JobApplicationDetailedResponse>, NotFound>> GetHandler(
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

    private static async Task<Results<Created<JobApplicationDetailedResponse>, ValidationProblem>> CreateHandler(
        OfferApplicationRequest offerApplicationRequest,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.CreateOffer(offerApplicationRequest, cancellationToken);

        return result.Match(
            onSuccess: jobApplication => TypedResults.Created($"{Prefix}/{jobApplication.Id}", jobApplication),
            onError: errors => TypedResults.ValidationProblem(errors)
        );
    }

    private static async Task<Results<Created<JobApplicationDetailedResponse>, ValidationProblem>> CreateSpontaneousHandler(
        SpontaneousApplicationRequest spontaneousApplicationRequest,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.CreateSpontaneous(spontaneousApplicationRequest, cancellationToken);

        return result.Match(
            onSuccess: jobApplication => TypedResults.Created($"{Prefix}/{jobApplication.Id}", jobApplication),
            onError: errors => TypedResults.ValidationProblem(errors)
        );
    }

    private static async Task<Results<Ok<JobApplicationDetailedResponse>, ValidationProblem, Conflict, NotFound>> UpdateHandler(
        Guid id,
        UpdateJobApplicationRequest updateRequest,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.Update(id, updateRequest, cancellationToken);

        // TODO Migrate later to fluid API
        // return result.ConvertToHttpResult()
        //     .IsUpdated()
        //     .OrHaveInvalidData()
        //     .OrAlreadyArchived()
        //     .OrFinallyNotFound();
        return result.Match(
            onSuccess: jobApplication => TypedResults.Ok(jobApplication),
            onInvalid: errors => TypedResults.ValidationProblem(errors),
            onArchived: _ => TypedResults.Conflict(),
            onError: _ => TypedResults.NotFound()
        );
    }

    private static async Task<Results<NoContent, Conflict, NotFound>> ArchiveHandler(
        Guid id,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.Archive(id, cancellationToken);

        return result.Match(
            onSuccess: _ => TypedResults.NoContent(),
            onInvalid: _ => TypedResults.Conflict(),
            onError: _ => TypedResults.NotFound()
        );
    }

    private static async Task<Results<NoContent, Conflict, NotFound>> ReopenHandler(
        Guid id,
        JobApplicationService jobApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.Reopen(id, cancellationToken);

        return result.Match(
            onSuccess: _ => TypedResults.NoContent(),
            onInvalid: _ => TypedResults.Conflict(),
            onError: _ => TypedResults.NotFound()
        );
    }

    private static async Task<Results<NoContent, ValidationProblem, Conflict, NotFound>>
        UpdateStatusHandler(
            Guid id,
            StatusRequest request,
            JobApplicationService jobApplicationService,
            CancellationToken cancellationToken)
    {
        var result = await jobApplicationService.ChangeStatus(id, request, cancellationToken);

        return result.Match(
            onSuccess: _ => TypedResults.NoContent(),
            onInvalid: errors => TypedResults.ValidationProblem(errors),
            onArchived: _ => TypedResults.Conflict(),
            onError: _ => TypedResults.NotFound());
    }
}
