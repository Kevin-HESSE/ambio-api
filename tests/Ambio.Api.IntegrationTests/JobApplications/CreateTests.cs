using System.Net;
using System.Net.Http.Json;

using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

using Microsoft.AspNetCore.Http;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class CreateTests : BaseJobApplicationTests
{
    public CreateTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldPersist()
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer());

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationResponse>(response);
        await AssertNumberOfJobApplicationsInDatabaseAsync(1);

        var persistedJobApplication = await GetPersistedJobApplicationAsync(content.Id);

        var offerApplication = Assert.IsType<OfferApplication>(persistedJobApplication);
        Assert.Equal(jobApplicationRequest.Title, offerApplication.Title);
        Assert.Equal(jobApplicationRequest.Company, offerApplication.Company);
        Assert.Equal(jobApplicationRequest.Url, offerApplication.Url);
        Assert.Equal(jobApplicationRequest.AppliedOn, offerApplication.AppliedOn);
        Assert.Equal(ApplicationStatus.Sent, offerApplication.Status);
        Assert.Null(offerApplication.ArchivedAt);
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldReturnTheCreatedOfferApplication()
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer());

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationResponse>(response);

        var expectedLocation = new Uri($"{BaseUrl}/{content.Id}", UriKind.Relative);
        Assert.Equal(expectedLocation, response.Headers.Location);

        Assert.Equal(jobApplicationRequest.Title, content.Title);
        Assert.Equal(jobApplicationRequest.Company, content.Company);
        Assert.Equal(jobApplicationRequest.Url, content.Url);
        Assert.Equal(jobApplicationRequest.AppliedOn, content.AppliedOn);
        Assert.Equal(nameof(ApplicationKind.Offer), content.Kind);
        Assert.Equal(ApplicationStatus.Sent, content.CurrentStatus);
        Assert.Null(content.ArchivedAt);
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldSetTimestamps()
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer());

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var expectedTimestamp = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var content = await ReadContentAsync<JobApplicationResponse>(response);

        var persistedJobApplication = await GetPersistedJobApplicationAsync(content.Id);
        Assert.Equal(expectedTimestamp, persistedJobApplication.CreatedAt);
        Assert.Equal(expectedTimestamp, persistedJobApplication.UpdatedAt);
    }

    [Fact]
    public async Task Create_WhenAppliedOnIsToday_ShouldReturnCreated()
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer()) with
        {
            AppliedOn = TestClock.TodayFromProvider(FakeTimeProvider)
        };

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenAppliedOnIsInTheFuture_ShouldReturnValidationProblem()
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer()) with
        {
            AppliedOn = TestClock.TodayFromProvider(FakeTimeProvider).AddDays(1)
        };

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, "appliedOn");
    }

    [Theory]
    [InlineData("title")]
    [InlineData("company")]
    [InlineData("url")]
    [InlineData("appliedOn")]
    public async Task Create_WhenPropertyIsMissing_ShouldReturnValidationProblem(string property)
    {
        var payload = GeneratePayload();
        payload.Remove(property);

        var response = await Client.PostAsJsonAsync(BaseUrl, payload, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, property);
    }

    [Theory]
    [InlineData("title", "")]
    [InlineData("title", "   ")]
    [InlineData("company", "")]
    [InlineData("company", "   ")]
    [InlineData("url", "")]
    public async Task Create_WhenPropertyIsEmpty_ShouldReturnValidationProblem(string property, string value)
    {
        var payload = GeneratePayload();
        payload[property] = value;

        var response = await Client.PostAsJsonAsync(BaseUrl, payload, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, property);
    }

    [Theory]
    [InlineData("acme.io/jobs/1")]
    [InlineData("/jobs/1")]
    [InlineData("ftp://acme.io/jobs/1")]
    [InlineData("mailto:jobs@acme.io")]
    [InlineData("https://acme")]
    [InlineData("https://192.168.1.1/jobs/1")]
    [InlineData("not an url")]
    public async Task Create_WhenUrlIsInvalid_ShouldReturnValidationProblem(string url)
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer()) with
        {
            Url = url
        };

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, "url");
    }

    [Theory]
    [InlineData("http://acme.io/jobs/1")]
    [InlineData("https://acme.io/jobs/1")]
    [InlineData("https://careers.acme.co.uk/jobs/1?ref=linkedin")]
    public async Task Create_WhenUrlIsValid_ShouldReturnCreated(string url)
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer()) with
        {
            Url = url
        };

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("19/11/2026")]
    [InlineData("2026-13-01")]
    [InlineData("not a date")]
    public async Task Create_WhenAppliedOnCannotBeParsed_ShouldReturnBadRequest(string appliedOn)
    {
        // A malformed date is rejected by the body binding, before FluentValidation
        var payload = GeneratePayload();
        payload["appliedOn"] = appliedOn;

        var response = await Client.PostAsJsonAsync(BaseUrl, payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNumberOfJobApplicationsInDatabaseAsync(0);
    }

    [Fact]
    public async Task Create_WhenSeveralPropertiesAreInvalid_ShouldReturnAllErrors()
    {
        var payload = new Dictionary<string, object?>
        {
            ["title"] = "",
            ["company"] = "",
            ["url"] = "acme",
            ["appliedOn"] = TestClock.TodayFromProvider(FakeTimeProvider).AddDays(1)
        };

        var response = await Client.PostAsJsonAsync(BaseUrl, payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadContentAsync<HttpValidationProblemDetails>(response);
        Assert.Equivalent(new[] { "title", "company", "url", "appliedOn" }, problem.Errors.Keys);
    }

    // The validator compares against the fake clock, which other tests may have advanced


    // A raw payload, to send missing or wrongly typed properties
    private static Dictionary<string, object?> GeneratePayload()
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer());

        return new Dictionary<string, object?>
        {
            ["title"] = jobApplicationRequest.Title,
            ["company"] = jobApplicationRequest.Company,
            ["url"] = jobApplicationRequest.Url,
            ["appliedOn"] = jobApplicationRequest.AppliedOn
        };
    }

    private async Task AssertValidationProblemAsync(HttpResponseMessage response, string expectedKey)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadContentAsync<HttpValidationProblemDetails>(response);
        Assert.Contains(expectedKey, problem.Errors.Keys);

        await AssertNumberOfJobApplicationsInDatabaseAsync(0);
    }
}
