using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Api.IntegrationTests.JobApplications;

public abstract class BaseJobApplicationTests : BaseIntegrationTests
{
    protected const string BaseUrl = "/job-applications";

    protected BaseJobApplicationTests(AmbioApiFactory factory) : base(factory)
    {
    }


    protected async Task<OfferApplication> CreateOfferApplicationAsync()
    {
        var jobApplication = FixturesFor.JobApplication.GetOffer();
        return await RegisterJobApplicationInDatabaseAsync(jobApplication);
    }

    // protected async Task<List<JobApplication>> CreateJobApplicationsAsync(int count)
    // {
    //     var jobApplications = FixturesFor.JobApplication.GetOffer(count);
    //     return await RegisterJobApplicationsInDatabaseAsync(jobApplications);
    // }

    protected async Task<OfferApplication> RegisterJobApplicationInDatabaseAsync(OfferApplication jobApplication)
    {
        DbContext.JobApplications.Add(jobApplication);
        await DbContext.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        return jobApplication;
    }

    protected async Task<List<JobApplication>> RegisterJobApplicationsInDatabaseAsync(List<JobApplication> jobApplications)
    {
        DbContext.JobApplications.AddRange(jobApplications);
        await DbContext.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        return jobApplications;
    }

    protected async Task AssertNumberOfJobApplicationsInDatabaseAsync(int count)
    {
        var numberOfJobApplications = await VerificationDbContext
            .JobApplications
            .CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(count, numberOfJobApplications);
    }

    protected Task<JobApplication> GetPersistedJobApplicationAsync(Guid id)
    {
        return VerificationDbContext
            .JobApplications
            .AsNoTracking()
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
    }

    protected static OfferApplicationRequest GenerateJobApplicationRequest(OfferApplication jobApplication,
        string? title = null)
    {
        return new OfferApplicationRequest(
            Title: title ?? jobApplication.Title,
            Company: jobApplication.Company,
            Url: jobApplication.Url,
            AppliedOn: jobApplication.AppliedOn);
    }
}
