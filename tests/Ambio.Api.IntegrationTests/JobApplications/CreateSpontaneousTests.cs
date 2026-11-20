using System.Net;
using System.Net.Http.Json;

using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class CreateSpontaneousTests : BaseCreateTests
{
    public CreateSpontaneousTests(AmbioApiFactory factory) : base(factory)
    {
    }

    protected override string Endpoint => $"{BaseUrl}/spontaneous";

    protected override ApplicationKind ExpectedKind => ApplicationKind.Spontaneous;

    protected override Dictionary<string, object?> GeneratePayload()
    {
        var request = GenerateSpontaneousRequest(FixturesFor.JobApplication.GetSpontaneous());

        return new Dictionary<string, object?>
        {
            ["title"] = request.Title,
            ["company"] = request.Company,
            ["appliedOn"] = request.AppliedOn
        };
    }

    protected override void AssertSpecificProperties(Dictionary<string, object?> payload, JobApplication persisted)
    {
        Assert.IsType<SpontaneousApplication>(persisted);
    }

    [Fact]
    public async Task Create_WhenUrlIsSent_ShouldIgnoreIt()
    {
        // The request has no url property, so an extra url in the body is dropped by the binding
        var payload = GeneratePayload();
        payload["url"] = "https://acme.io/jobs/1";

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationResponse>(response);
        Assert.Null(content.Url);
        Assert.Equal(nameof(ApplicationKind.Spontaneous), content.Kind);

        var persistedJobApplication = await GetPersistedJobApplicationAsync(content.Id);
        Assert.IsType<SpontaneousApplication>(persistedJobApplication);
    }
}
