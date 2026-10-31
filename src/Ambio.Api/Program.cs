using Ambio.Api.Data;
using Ambio.Api.Models;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AmbioDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseSnakeCaseNamingConvention();
});

var app = builder.Build();

//TODO need to be remove when migrations are in place
var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AmbioDbContext>();
context.Database.EnsureDeleted();
context.Database.EnsureCreated();

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
    async (AmbioDbContext ambioDbContext) => await ambioDbContext.JobApplications.ToListAsync());

app.MapGet(
    "/job-applications/{id:guid}",
    async Task<IResult> (Guid id, AmbioDbContext ambioDbContext) =>
    {
        // Serves match from memory, otherwise it will be fetched from database
        var jobApplication = await ambioDbContext.JobApplications.FindAsync(id);

        return jobApplication is not null
            ? TypedResults.Ok(jobApplication)
            : TypedResults.NotFound();
    });

app.MapPost(
    "/job-applications",
    async Task<IResult> (JobApplication jobApplication, AmbioDbContext ambioDbContext) =>
    {
        await ambioDbContext.JobApplications.AddAsync(jobApplication);
        await ambioDbContext.SaveChangesAsync();

        return TypedResults.Created($"/job-applications/{jobApplication.Id}", jobApplication);
    });

app.MapPut(
    "/job-applications/{id:guid}",
    async Task<IResult> (Guid id, JobApplication jobApplication, AmbioDbContext ambioDbContext) =>
    {
        var existingApplication = await ambioDbContext.JobApplications.FindAsync(id);

        if (existingApplication is null) return TypedResults.NotFound();

        existingApplication.Title = jobApplication.Title;
        existingApplication.Url = jobApplication.Url;
        existingApplication.Company = jobApplication.Company;
        existingApplication.Status = jobApplication.Status;
        existingApplication.AppliedOn = jobApplication.AppliedOn;
        existingApplication.UpdatedAt = DateTime.UtcNow;

        await ambioDbContext.SaveChangesAsync();

        return TypedResults.Ok(existingApplication);
    });

app.MapDelete(
    "/job-applications/{id:guid}",
    async Task<IResult> (Guid id, AmbioDbContext ambioDbContext) =>
    {
        var existingApplication = await ambioDbContext.JobApplications.FindAsync(id);

        if (existingApplication is null) return TypedResults.NotFound();

        ambioDbContext.JobApplications.Remove(existingApplication);
        await ambioDbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    });

app.Run();

