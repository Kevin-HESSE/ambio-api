using System.Net;

using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Ambio.Api.IntegrationTests.JobApplications;

public abstract class BaseJobApplicationTests : BaseIntegrationTests
{
    protected const string BaseUrl = "/job-applications";

    protected BaseJobApplicationTests(AmbioApiFactory factory) : base(factory)
    {
    }


    protected async Task<OfferApplication> CreateOfferApplicationAsync(bool archived = false)
    {
        var jobApplication = FixturesFor.JobApplication.GetOffer(archived);
        return await RegisterJobApplicationInDatabaseAsync(jobApplication);
    }

    protected async Task<List<OfferApplication>> CreateOfferApplicationAsync(int count)
    {
        var jobApplications = FixturesFor.JobApplication.GetOffer(count);
        return await RegisterJobApplicationsInDatabaseAsync(jobApplications);
    }

    protected async Task<SpontaneousApplication> CreateSpontaneousApplicationAsync(bool archived = false)
    {
        var jobApplication = FixturesFor.JobApplication.GetSpontaneous(archived);
        return await RegisterJobApplicationInDatabaseAsync(jobApplication);
    }

    protected async Task<List<SpontaneousApplication>> CreateSpontaneousApplicationAsync(int count)
    {
        var jobApplications = FixturesFor.JobApplication.GetSpontaneous(count);
        return await RegisterJobApplicationsInDatabaseAsync(jobApplications);
    }

    protected async Task<JobApplication> CreateJobApplicationAsync(ApplicationKind kind, bool archived = false)
    {
        return kind switch
        {
            ApplicationKind.Offer => await CreateOfferApplicationAsync(archived),
            ApplicationKind.Spontaneous => await CreateSpontaneousApplicationAsync(archived),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    protected async Task<IEnumerable<JobApplication>> CreateJobApplicationAsync(ApplicationKind kind, int count)
    {
        return kind switch
        {
            ApplicationKind.Offer => await CreateOfferApplicationAsync(count),
            ApplicationKind.Spontaneous => await CreateSpontaneousApplicationAsync(count),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    protected async Task<T> RegisterJobApplicationInDatabaseAsync<T>(T jobApplication) where T : JobApplication
    {
        DbContext.JobApplications.Add(jobApplication);
        await DbContext.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        return jobApplication;
    }

    protected async Task<List<T>> RegisterJobApplicationsInDatabaseAsync<T>(List<T> jobApplications) where T : JobApplication
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

    protected static SpontaneousApplicationRequest GenerateSpontaneousRequest(SpontaneousApplication jobApplication)
    {
        return new SpontaneousApplicationRequest(
            Title: jobApplication.Title,
            Company: jobApplication.Company,
            AppliedOn: jobApplication.AppliedOn);
    }

    protected async Task AssertValidationProblemAsync(HttpResponseMessage response, string expectedKey)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadContentAsync<HttpValidationProblemDetails>(response);
        Assert.Contains(expectedKey, problem.Errors.Keys);

        await AssertNumberOfJobApplicationsInDatabaseAsync(0);
    }
}
