using System.Net;
using System.Net.Http.Json;

using Ambio.Api.Models;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class UpdateTests : BaseJobApplicationTests
{
    public UpdateTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Update_WhenJobApplicationExists_ShouldReturnOk()
    {
        var jobApplication = await CreateJobApplicationAsync();

        jobApplication.Title = "Updated Title";
        var response = await Client.PutAsJsonAsync($"{BaseUrl}/{jobApplication.Id}", jobApplication, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<JobApplication>(response);
        var persisted = await VerificationDbContext.JobApplications
            .FirstAsync(x => x.Id == jobApplication.Id, TestContext.Current.CancellationToken);

        Assert.Equal(jobApplication.Title, content.Title);
        Assert.Equal(jobApplication.Title, persisted.Title);
    }

    [Fact]
    public async Task Update_WhenJobApplicationDoesNotExist_ShouldReturnNotFound()
    {
        var jobApplication = await CreateJobApplicationAsync();

        var fakeGuid = Guid.CreateVersion7();
        var response = await Client.PutAsJsonAsync($"{BaseUrl}/{fakeGuid}", jobApplication, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
