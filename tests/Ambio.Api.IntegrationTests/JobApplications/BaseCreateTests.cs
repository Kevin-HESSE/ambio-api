using System.Net;
using System.Net.Http.Json;

using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

using Microsoft.AspNetCore.Http;

namespace Ambio.Api.IntegrationTests.JobApplications;

// Tests shared by the creation routes. Each route only differs by its url, its payload and its kind.
public abstract class BaseCreateTests : BaseJobApplicationTests
{
    protected BaseCreateTests(AmbioApiFactory factory) : base(factory)
    {
    }

    protected abstract string Endpoint { get; }

    protected abstract ApplicationKind ExpectedKind { get; }

    // A raw payload, to send missing or wrongly typed properties
    protected abstract Dictionary<string, object?> GeneratePayload();

    // Checks the persisted type and the properties specific to the route
    protected abstract void AssertSpecificProperties(Dictionary<string, object?> payload, JobApplication persisted);

    // Makes the properties specific to the route invalid, and returns their keys
    protected virtual IReadOnlyCollection<string> InvalidateSpecificProperties(Dictionary<string, object?> payload)
    {
        return [];
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldPersist()
    {
        var payload = GeneratePayload();

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationResponse>(response);
        await AssertNumberOfJobApplicationsInDatabaseAsync(1);

        var persistedJobApplication = await GetPersistedJobApplicationAsync(content.Id);

        AssertSpecificProperties(payload, persistedJobApplication);
        Assert.Equal((string?)payload["title"], persistedJobApplication.Title);
        Assert.Equal((string?)payload["company"], persistedJobApplication.Company);
        Assert.Equal((DateOnly?)payload["appliedOn"], persistedJobApplication.AppliedOn);
        Assert.Equal(ApplicationStatus.Sent, persistedJobApplication.Status);
        Assert.Null(persistedJobApplication.ArchivedAt);

        var entry = Assert.Single(await GetPersistedHistoryAsync(content.Id));
        Assert.Equal(ApplicationStatus.Created, entry.StatusFrom);
        Assert.Equal(ApplicationStatus.Sent, entry.StatusTo);
        Assert.Equal((DateOnly?)payload["appliedOn"], entry.ChangedAt);
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldReturnTheCreatedJobApplication()
    {
        var payload = GeneratePayload();

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationDetailedResponse>(response);

        var expectedLocation = new Uri($"{BaseUrl}/{content.Id}", UriKind.Relative);
        Assert.Equal(expectedLocation, response.Headers.Location);

        Assert.Equal((string?)payload["title"], content.Title);
        Assert.Equal((string?)payload["company"], content.Company);
        Assert.Equal((string?)payload.GetValueOrDefault("url"), content.Url);
        Assert.Equal((DateOnly?)payload["appliedOn"], content.AppliedOn);
        Assert.Equal(ExpectedKind.ToString(), content.Kind);
        Assert.Equal(ApplicationStatus.Sent, content.CurrentStatus);
        Assert.Null(content.ArchivedAt);

        var entry = Assert.Single(content.History);
        Assert.Equal(ApplicationStatus.Sent, entry.Status);
        Assert.Equal((DateOnly?)payload["appliedOn"], entry.Date);
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldSetTimestamps()
    {
        var payload = GeneratePayload();

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);
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
        var payload = GeneratePayload();
        payload["appliedOn"] = TestClock.TodayFromProvider(FakeTimeProvider);

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenAppliedOnIsInTheFuture_ShouldReturnValidationProblem()
    {
        var payload = GeneratePayload();
        payload["appliedOn"] = TestClock.TodayFromProvider(FakeTimeProvider).AddDays(1);

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, "appliedOn");
    }

    [Theory]
    [InlineData("title")]
    [InlineData("company")]
    [InlineData("appliedOn")]
    public async Task Create_WhenPropertyIsMissing_ShouldReturnValidationProblem(string property)
    {
        var payload = GeneratePayload();
        payload.Remove(property);

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, property);
    }

    [Theory]
    [InlineData("title", "")]
    [InlineData("title", "   ")]
    [InlineData("company", "")]
    [InlineData("company", "   ")]
    public async Task Create_WhenPropertyIsEmpty_ShouldReturnValidationProblem(string property, string value)
    {
        var payload = GeneratePayload();
        payload[property] = value;

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, property);
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

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNumberOfJobApplicationsInDatabaseAsync(0);
    }

    [Fact]
    public async Task Create_WhenSeveralPropertiesAreInvalid_ShouldReturnAllErrors()
    {
        var payload = GeneratePayload();
        payload["title"] = "";
        payload["company"] = "";
        payload["appliedOn"] = TestClock.TodayFromProvider(FakeTimeProvider).AddDays(1);
        var specificKeys = InvalidateSpecificProperties(payload);

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadContentAsync<HttpValidationProblemDetails>(response);
        Assert.Equivalent(new[] { "title", "company", "appliedOn" }.Concat(specificKeys), problem.Errors.Keys);
    }
}
