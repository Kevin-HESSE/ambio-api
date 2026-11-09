using System.Net;
using System.Net.Http.Json;

using Ambio.Application.JobApplications;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class CreateTests : BaseJobApplicationTests
{
    public CreateTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldReturnCreated()
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(JobApplicationGenerator.Generate());

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await AssertNumberOfJobApplicationsInDatabaseAsync(1);
        var content = await ReadContentAsync<JobApplicationResponse>(response);

        var expectedLocation = new Uri($"{BaseUrl}/{content.Id}", UriKind.Relative);
        Assert.Equal(expectedLocation, response.Headers.Location);

        var jobApplication = await GetPersistedJobApplicationAsync(content.Id);
        Assert.Equal(jobApplication.Title, content.Title);
    }

    [Fact]
    public async Task Create_WithValidPayload_ShouldSetTimestamps()
    {
        var jobApplicationRequest = GenerateJobApplicationRequest(JobApplicationGenerator.Generate());

        var response = await Client.PostAsJsonAsync(BaseUrl, jobApplicationRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var expectedTimestamp = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var content = await ReadContentAsync<JobApplicationResponse>(response);

        var persistedJobApplication = await GetPersistedJobApplicationAsync(content.Id);
        Assert.Equal(expectedTimestamp, persistedJobApplication.CreatedAt);
        Assert.Equal(expectedTimestamp, persistedJobApplication.UpdatedAt);
    }
}
