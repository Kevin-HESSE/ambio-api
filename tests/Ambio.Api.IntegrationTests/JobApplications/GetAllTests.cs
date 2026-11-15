using System.Net;

using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

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
    public async Task GetAll_WhenNoFilterApplied_ShouldReturnTheTenFirstItems()
    {
        await SetUpFixturesAsync();

        var response = await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<List<JobApplicationResponse>>(response);

        Assert.Equal(10, content.Count);
        Assert.Contains(content, x => x.ArchivedAt is null);
    }

    private async Task SetUpFixturesAsync()
    {
        List<JobApplication> jobApplications =
        [
            ..FixturesFor.JobApplication.GetOffer(count : 50),
            ..FixturesFor.JobApplication.GetOffer(count : 10, archived: true),
            ..FixturesFor.JobApplication.GetSpontaneous(count :50),
            ..FixturesFor.JobApplication.GetSpontaneous(count :50, archived: true),
        ];

        await RegisterJobApplicationsInDatabaseAsync(jobApplications);
    }
}
