using System.Net;
using System.Net.Http.Json;

using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Domain.JobApplications;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class CreateOfferTests : BaseCreateTests
{
    public CreateOfferTests(AmbioApiFactory factory) : base(factory)
    {
    }

    protected override string Endpoint => BaseUrl;

    protected override ApplicationKind ExpectedKind => ApplicationKind.Offer;

    protected override Dictionary<string, object?> GeneratePayload()
    {
        var request = GenerateJobApplicationRequest(FixturesFor.JobApplication.GetOffer());

        return new Dictionary<string, object?>
        {
            ["title"] = request.Title,
            ["company"] = request.Company,
            ["url"] = request.Url,
            ["appliedOn"] = request.AppliedOn
        };
    }

    protected override void AssertSpecificProperties(Dictionary<string, object?> payload, JobApplication persisted)
    {
        var offerApplication = Assert.IsType<OfferApplication>(persisted);
        Assert.Equal((string?)payload["url"], offerApplication.Url);
    }

    protected override IReadOnlyCollection<string> InvalidateSpecificProperties(Dictionary<string, object?> payload)
    {
        payload["url"] = "acme";
        return ["url"];
    }

    [Fact]
    public async Task Create_WhenUrlIsMissing_ShouldReturnValidationProblem()
    {
        var payload = GeneratePayload();
        payload.Remove("url");

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, "url");
    }

    [Theory]
    [InlineData("")]
    [InlineData("acme.io/jobs/1")]
    [InlineData("/jobs/1")]
    [InlineData("ftp://acme.io/jobs/1")]
    [InlineData("mailto:jobs@acme.io")]
    [InlineData("https://acme")]
    [InlineData("https://192.168.1.1/jobs/1")]
    [InlineData("not an url")]
    public async Task Create_WhenUrlIsInvalid_ShouldReturnValidationProblem(string url)
    {
        var payload = GeneratePayload();
        payload["url"] = url;

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);

        await AssertValidationProblemAsync(response, "url");
    }

    [Theory]
    [InlineData("http://acme.io/jobs/1")]
    [InlineData("https://acme.io/jobs/1")]
    [InlineData("https://careers.acme.co.uk/jobs/1?ref=linkedin")]
    public async Task Create_WhenUrlIsValid_ShouldReturnCreated(string url)
    {
        var payload = GeneratePayload();
        payload["url"] = url;

        var response = await Client.PostAsJsonAsync(Endpoint, payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
