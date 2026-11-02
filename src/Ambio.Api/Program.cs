using Ambio.Api.Data;
using Ambio.Api.Dtos;
using Ambio.Api.Models;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TimestampInterceptor>();
builder.Services.AddDbContext<AmbioDbContext>((provider, options) =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        options.UseSnakeCaseNamingConvention();
        options.AddInterceptors(provider.GetRequiredService<TimestampInterceptor>());
    },
    contextLifetime: ServiceLifetime.Scoped,
    optionsLifetime: ServiceLifetime.Singleton);

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
    async Task<Results<BadRequest, Created<JobApplicationResponse>>> (JobApplication jobApplication, AmbioDbContext ambioDbContext) =>
    {
        await ambioDbContext.JobApplications.AddAsync(jobApplication);
        await ambioDbContext.SaveChangesAsync();

        return TypedResults.Created($"/job-applications/{jobApplication.Id}", jobApplication.ToResponse());
    });

app.MapPut(
    "/job-applications/{id:guid}",
    async Task<Results<NotFound, BadRequest, Ok<JobApplicationResponse>>> (Guid id, JobApplication jobApplication, AmbioDbContext ambioDbContext) =>
    {
        var existingApplication = await ambioDbContext.JobApplications.FindAsync(id);

        if (existingApplication is null) return TypedResults.NotFound();

        existingApplication.Title = jobApplication.Title;
        existingApplication.Url = jobApplication.Url;
        existingApplication.Company = jobApplication.Company;
        existingApplication.Status = jobApplication.Status;
        existingApplication.AppliedOn = jobApplication.AppliedOn;

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

