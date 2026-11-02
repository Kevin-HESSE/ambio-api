using System.Net;

using Ambio.Api.Dtos;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class GetAllTests : BaseJobApplicationTests
{
    public GetAllTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_WhenNoJobApplicationsExist_ShouldReturnEmpty()
    {
        var response = await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<List<JobApplicationResponse>>(response);
        Assert.Empty(content);
    }

    [Fact]
    public async Task GetAll_WhenJobApplicationsExist_ShouldReturnAll()
    {
        const int count = 10;
        await CreateJobApplicationsAsync(count);

        var response = await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<List<JobApplicationResponse>>(response);
        Assert.Equal(count, content.Count);
    }
}
