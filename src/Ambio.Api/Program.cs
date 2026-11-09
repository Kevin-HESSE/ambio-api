using Ambio.Api.Dtos;
using Ambio.Api.Models;
using Ambio.Data;

using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAmbioContext(
    migrationAssembly: "Ambio.Migrations",
    connectionString: builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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
    async (AmbioDbContext ambioDbContext) => await ambioDbContext.JobApplications
        .Select(ja => ja.ToResponse())
        .ToListAsync()
);

app.MapGet(
    "/job-applications/{id:guid}",
    async Task<Results<NotFound, Ok<JobApplicationResponse>>> (Guid id, AmbioDbContext ambioDbContext) =>
    {
        var jobApplication = await ambioDbContext.JobApplications.FindAsync(id);

        return jobApplication is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(jobApplication.ToResponse());
    });

app.MapPost(
    "/job-applications",
    async Task<Results<ValidationProblem, Created<JobApplicationResponse>>> (
        JobApplicationRequest jobApplicationRequest,
        AmbioDbContext ambioDbContext,
        IValidator<JobApplicationRequest> validator) =>
    {
        var validationResult = await validator.ValidateAsync(jobApplicationRequest);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var jobApplication = jobApplicationRequest.ToEntity();
        await ambioDbContext.JobApplications.AddAsync(jobApplication);
        await ambioDbContext.SaveChangesAsync();

        return TypedResults.Created($"/job-applications/{jobApplication.Id}", jobApplication.ToResponse());
    });

app.MapPut(
    "/job-applications/{id:guid}",
    async Task<Results<NotFound, ValidationProblem, Ok<JobApplicationResponse>>> (
        Guid id,
        JobApplicationRequest jobApplicationRequest,
        AmbioDbContext ambioDbContext,
        IValidator<JobApplicationRequest> validator) =>
    {
        var validationResult = await validator.ValidateAsync(jobApplicationRequest);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var existingApplication = await ambioDbContext.JobApplications.FindAsync(id);

        if (existingApplication is null)
        {
            return TypedResults.NotFound();
        }

        existingApplication.Title = jobApplicationRequest.Title;
        existingApplication.Url = jobApplicationRequest.Url;
        existingApplication.Company = jobApplicationRequest.Company;
        existingApplication.Status = jobApplicationRequest.Status;
        existingApplication.AppliedOn = jobApplicationRequest.AppliedAt;

        await ambioDbContext.SaveChangesAsync();

        return TypedResults.Ok(existingApplication.ToResponse());
    });

app.MapDelete(
    "/job-applications/{id:guid}",
    async Task<Results<NotFound, NoContent>> (Guid id, AmbioDbContext ambioDbContext) =>
    {
        var existingApplication = await ambioDbContext.JobApplications.FindAsync(id);

        if (existingApplication is null) return TypedResults.NotFound();

        ambioDbContext.JobApplications.Remove(existingApplication);
        await ambioDbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    });

app.Run();
