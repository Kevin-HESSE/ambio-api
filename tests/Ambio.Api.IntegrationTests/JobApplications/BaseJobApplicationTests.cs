using Ambio.Api.Dtos;
using Ambio.Api.Models;

using Bogus;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Api.IntegrationTests.JobApplications;

public abstract class BaseJobApplicationTests : BaseIntegrationTests
{
    protected readonly Faker<JobApplication> JobApplicationGenerator = new Faker<JobApplication>()
        .RuleFor(j => j.Title, f => f.Lorem.Word())
        .RuleFor(j => j.Company, f => f.Company.CompanyName())
        .RuleFor(j => j.Url, f => f.Internet.Url());

    protected const string BaseUrl = "/job-applications";
    protected BaseJobApplicationTests(AmbioApiFactory factory) : base(factory)
    {
    }

    protected async Task<JobApplication> CreateJobApplicationAsync()
    {
        var jobApplication = JobApplicationGenerator.Generate();
        DbContext.JobApplications.Add(jobApplication);
        await DbContext.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        return jobApplication;
    }

    protected async Task<List<JobApplication>> CreateJobApplicationsAsync(int count)
    {
        var jobApplications = JobApplicationGenerator.Generate(count);
        DbContext.JobApplications.AddRange(jobApplications);
        await DbContext.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        return jobApplications;
    }

    protected async Task AssertNumberOfJobApplicationsInDatabaseAsync(int count)
    {
        var numberOfJobApplications = await VerificationDbContext.JobApplications
            .CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(count, numberOfJobApplications);
    }

    protected Task<JobApplication> GetPersistedJobApplicationAsync(Guid id)
    {
        return VerificationDbContext.JobApplications
            .AsNoTracking()
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
    }

    protected static JobApplicationRequest GenerateJobApplicationRequest(JobApplication jobApplication, string? title = null)
    {
        return new JobApplicationRequest(
            Title: title ?? jobApplication.Title,
            Company: jobApplication.Company,
            Url: jobApplication.Url,
            Status: jobApplication.Status,
            AppliedAt: jobApplication.AppliedOn);
    }
}
