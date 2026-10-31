using System.Net;

using Ambio.Api.Models;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class GetByIdTests : BaseJobApplicationTests
{
    public GetByIdTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetById_WhenJobApplicationExists_ShouldReturnOk()
    {
        var jobApplication = await CreateJobApplicationAsync();

        var response = await Client.GetAsync($"{BaseUrl}/{jobApplication.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<JobApplication>(response);
        Assert.Equal(jobApplication.Title, content.Title);
    }

    [Fact]
    public async Task GetById_WhenJobApplicationDoesNotExist_ShouldReturnNotFound()
    {
        var fakeGuid = Guid.CreateVersion7();
        var response = await Client.GetAsync($"{BaseUrl}/{fakeGuid}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
