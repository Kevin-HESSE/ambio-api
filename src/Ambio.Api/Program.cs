using Ambio.Application;
using Ambio.Application.JobApplications;
using Ambio.Data;

using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAmbioContext(
    migrationAssembly: "Ambio.Migrations",
    connectionString: builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddAmbioServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();

app.MapGet(
    "/job-applications",
    async (JobApplicationServices jobApplicationServices) =>
        await jobApplicationServices.GetAll()
);

app.MapGet(
    "/job-applications/{id:guid}",
    async Task<Results<NotFound, Ok<JobApplicationResponse>>> (Guid id, JobApplicationServices jobApplicationServices) =>
    {
        var jobApplication = await jobApplicationServices.GetById(id);

        return jobApplication is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(jobApplication);
    });

app.MapPost(
    "/job-applications",
    async Task<Results<ValidationProblem, Created<JobApplicationResponse>>> (
        JobApplicationRequest jobApplicationRequest,
        JobApplicationServices jobApplicationServices) =>
    {
        var validationResult = await jobApplicationServices.Validate(jobApplicationRequest);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var jobApplication = await jobApplicationServices.Create(jobApplicationRequest);

        return TypedResults.Created($"/job-applications/{jobApplication.Id}", jobApplication);
    });

app.MapPut(
    "/job-applications/{id:guid}",
    async Task<Results<NotFound, ValidationProblem, Ok<JobApplicationResponse>>> (
        Guid id,
        JobApplicationRequest jobApplicationRequest,
        JobApplicationServices jobApplicationServices) =>
    {
        var validationResult = await jobApplicationServices.Validate(jobApplicationRequest);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var existingApplication = await jobApplicationServices.Update(id, jobApplicationRequest);

        return existingApplication is null
            ? (Results<NotFound, ValidationProblem, Ok<JobApplicationResponse>>)TypedResults.NotFound()
            : (Results<NotFound, ValidationProblem, Ok<JobApplicationResponse>>)TypedResults.Ok(existingApplication);
    });

app.MapDelete(
    "/job-applications/{id:guid}",
    async Task<Results<NotFound, NoContent>> (Guid id, JobApplicationServices jobApplicationServices) =>
    {
        var isApplicationDeleted = await jobApplicationServices.Delete(id);

        return isApplicationDeleted
            ? (Results<NotFound, NoContent>)TypedResults.NoContent()
            : (Results<NotFound, NoContent>)TypedResults.NotFound();
    });

app.Run();
