using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

using Bogus;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Api.IntegrationTests.JobApplications;

public abstract class BaseJobApplicationTests : BaseIntegrationTests
{
    protected readonly Faker<OfferApplication> OfferApplicationGenerator = new Faker<OfferApplication>()
        .RuleFor(j => j.Title, f => f.Lorem.Word())
        .RuleFor(j => j.Company, f => f.Company.CompanyName())
        .RuleFor(j => j.Url, f => f.Internet.Url());

    protected const string BaseUrl = "/job-applications";
    protected BaseJobApplicationTests(AmbioApiFactory factory) : base(factory)
    {
    }

    protected async Task<OfferApplication> CreateJobApplicationAsync()
    {
        var jobApplication = OfferApplicationGenerator.Generate();
        DbContext.JobApplications.Add(jobApplication);
        await DbContext.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        return jobApplication;
    }

    protected async Task<List<OfferApplication>> CreateJobApplicationsAsync(int count)
    {
        var jobApplications = OfferApplicationGenerator.Generate(count);
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

    protected static JobApplicationRequest GenerateJobApplicationRequest(OfferApplication jobApplication, string? title = null)
    {
        return new JobApplicationRequest(
            Title: title ?? jobApplication.Title,
            Company: jobApplication.Company,
            Kind: "offer",
            Url: jobApplication.Url,
            Status: jobApplication.Status,
            AppliedAt: jobApplication.AppliedOn);
    }
}
