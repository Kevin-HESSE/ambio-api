using System.Net;

using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class GetByIdTests : BaseJobApplicationTests
{
    public GetByIdTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetById_WhenJobApplicationExists_ShouldReturnOk()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);

        var response = await Client.GetAsync($"{BaseUrl}/{jobApplication.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationDetailedResponse>(response);
        Assert.Equal(jobApplication.Title, content.Title);

        var entry = Assert.Single(content.History);
        Assert.Equal(ApplicationStatus.Sent, entry.Status);
        Assert.Equal(jobApplication.AppliedOn, entry.Date);
    }

    [Fact]
    public async Task GetById_WhenJobApplicationIsArchived_ShouldReturnHistory()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Interview, archived: true);

        var response = await Client.GetAsync($"{BaseUrl}/{jobApplication.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationDetailedResponse>(response);
        Assert.NotNull(content.ArchivedAt);
        Assert.Equal([ApplicationStatus.Sent, ApplicationStatus.Interview], content.History.Select(h => h.Status));
    }

    [Fact]
    public async Task GetById_WhenJobApplicationDoesNotExist_ShouldReturnNotFound()
    {
        var fakeGuid = Guid.CreateVersion7();
        var response = await Client.GetAsync($"{BaseUrl}/{fakeGuid}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
