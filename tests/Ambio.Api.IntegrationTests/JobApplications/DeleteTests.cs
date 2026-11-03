using System.Net;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class DeleteTests : BaseJobApplicationTests
{
    public DeleteTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Delete_WhenJobApplicationExists_ShouldReturnNoContent()
    {
        var jobApplication = await CreateJobApplicationAsync();

        var response = await Client.DeleteAsync($"{BaseUrl}/{jobApplication.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await AssertNumberOfJobApplicationsInDatabaseAsync(0);
    }

    [Fact]
    public async Task Delete_WhenJobApplicationDoesNotExist_ShouldReturnNotFound()
    {
        var fakeGuid = Guid.CreateVersion7();
        var response = await Client.DeleteAsync($"{BaseUrl}/{fakeGuid}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
