using System.Net;
using System.Net.Http.Json;

using Ambio.Api.Models;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class CreateTests : BaseJobApplicationTests
{
    public CreateTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldReturnCreated()
    {
        var jobApplication = JobApplicationGenerator.Generate();

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplication, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await AssertNumberOfJobApplicationsInDatabaseAsync(1);
        var content = await ReadContentAsync<JobApplication>(response);

        var expectedLocation = new Uri($"{BaseUrl}/{content.Id}", UriKind.Relative);
        Assert.Equal(expectedLocation, response.Headers.Location);

        Assert.Equal(jobApplication.Title, content.Title);
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldSetTimestamps()
    {
        var jobApplication = JobApplicationGenerator.Generate();

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplication, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var expectedTimestamp = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var content = await ReadContentAsync<JobApplication>(response);
        Assert.Equal(expectedTimestamp, content.CreatedAt);
        Assert.Equal(expectedTimestamp, content.UpdatedAt);

        var persistedJobApplication = await GetPersistedJobApplicationAsync(content.Id);
        Assert.Equal(expectedTimestamp, persistedJobApplication.CreatedAt);
        Assert.Equal(expectedTimestamp, persistedJobApplication.UpdatedAt);
    }
}
