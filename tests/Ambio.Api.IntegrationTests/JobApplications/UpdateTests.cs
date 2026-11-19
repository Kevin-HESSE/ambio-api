using System.Net;
using System.Net.Http.Json;

using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

using Microsoft.AspNetCore.Http;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class UpdateTests : BaseJobApplicationTests
{
    private const string UpdatedUrl = "https://careers.acme.io/jobs/updated";

    public UpdateTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Update_WhenOfferApplicationExists_ShouldReturnTheUpdatedJobApplication()
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);

        var response = await PutAsync(jobApplication.Id, payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationResponse>(response);
        Assert.Equal(jobApplication.Id, content.Id);
        Assert.Equal((string?)payload["title"], content.Title);
        Assert.Equal((string?)payload["company"], content.Company);
        Assert.Equal(UpdatedUrl, content.Url);
        Assert.Equal(nameof(ApplicationKind.Offer), content.Kind);
        Assert.Equal(jobApplication.Status, content.CurrentStatus);
        Assert.Equal(jobApplication.AppliedOn, content.AppliedOn);
    }

    [Fact]
    public async Task Update_WhenOfferApplicationExists_ShouldPersistChanges()
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);

        var response = await PutAsync(jobApplication.Id, payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persisted = Assert.IsType<OfferApplication>(await GetPersistedJobApplicationAsync(jobApplication.Id));
        Assert.Equal((string?)payload["title"], persisted.Title);
        Assert.Equal((string?)payload["company"], persisted.Company);
        Assert.Equal(UpdatedUrl, persisted.Url);
        Assert.Equal(jobApplication.Status, persisted.Status);
        Assert.Equal(jobApplication.AppliedOn, persisted.AppliedOn);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_WhenSpontaneousApplicationExists_ShouldPersistChanges(bool sendNullUrl)
    {
        var jobApplication = await CreateSpontaneousApplicationAsync();
        var payload = GenerateSpontaneousPayload(jobApplication);

        if (!sendNullUrl)
        {
            payload.Remove("url");
        }

        var response = await PutAsync(jobApplication.Id, payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationResponse>(response);
        Assert.Equal((string?)payload["title"], content.Title);
        Assert.Equal((string?)payload["company"], content.Company);
        Assert.Null(content.Url);
        Assert.Equal(nameof(ApplicationKind.Spontaneous), content.Kind);

        var persisted = Assert.IsType<SpontaneousApplication>(await GetPersistedJobApplicationAsync(jobApplication.Id));
        Assert.Equal((string?)payload["title"], persisted.Title);
        Assert.Equal((string?)payload["company"], persisted.Company);
    }

    [Fact]
    public async Task Update_WhenJobApplicationDoesNotExist_ShouldReturnNotFound()
    {
        var jobApplication = await CreateOfferApplicationAsync();

        var response = await PutAsync(Guid.CreateVersion7(), GenerateOfferPayload(jobApplication));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenJobApplicationExists_ShouldUpdateTimestamps()
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var expectedCreatedAt = FakeTimeProvider.GetUtcNow().UtcDateTime;
        FakeTimeProvider.Advance(TimeSpan.FromDays(1));
        var expectedUpdatedAt = FakeTimeProvider.GetUtcNow().UtcDateTime;

        var response = await PutAsync(jobApplication.Id, GenerateOfferPayload(jobApplication));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persisted = await GetPersistedJobApplicationAsync(jobApplication.Id);
        Assert.Equal(expectedCreatedAt, persisted.CreatedAt);
        Assert.Equal(expectedUpdatedAt, persisted.UpdatedAt);
    }

    [Fact]
    public async Task Update_WhenAppliedOnIsSent_ShouldIgnoreIt()
    {
        // The request has no appliedOn property, so an extra appliedOn in the body is dropped by the binding
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);
        payload["appliedOn"] = jobApplication.AppliedOn.AddDays(-1);

        var response = await PutAsync(jobApplication.Id, payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persisted = await GetPersistedJobApplicationAsync(jobApplication.Id);
        Assert.Equal(jobApplication.AppliedOn, persisted.AppliedOn);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("company")]
    public async Task Update_WhenPropertyIsMissing_ShouldReturnValidationProblem(string property)
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);
        payload.Remove(property);

        var response = await PutAsync(jobApplication.Id, payload);

        await AssertNotUpdatedAsync(response, jobApplication, property);
    }

    [Theory]
    [InlineData("title", "")]
    [InlineData("title", "   ")]
    [InlineData("company", "")]
    [InlineData("company", "   ")]
    public async Task Update_WhenPropertyIsEmpty_ShouldReturnValidationProblem(string property, string value)
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);
        payload[property] = value;

        var response = await PutAsync(jobApplication.Id, payload);

        await AssertNotUpdatedAsync(response, jobApplication, property);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_WhenOfferUrlIsNullOrMissing_ShouldReturnValidationProblem(bool sendNullUrl)
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);

        if (sendNullUrl)
        {
            payload["url"] = null;
        }
        else
        {
            payload.Remove("url");
        }

        var response = await PutAsync(jobApplication.Id, payload);

        await AssertNotUpdatedAsync(response, jobApplication, "url");
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
    public async Task Update_WhenOfferUrlIsInvalid_ShouldReturnValidationProblem(string url)
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);
        payload["url"] = url;

        var response = await PutAsync(jobApplication.Id, payload);

        await AssertNotUpdatedAsync(response, jobApplication, "url");
    }

    [Theory]
    [InlineData("http://acme.io/jobs/1")]
    [InlineData("https://acme.io/jobs/1")]
    [InlineData("https://careers.acme.co.uk/jobs/1?ref=linkedin")]
    public async Task Update_WhenOfferUrlIsValid_ShouldReturnOk(string url)
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);
        payload["url"] = url;

        var response = await PutAsync(jobApplication.Id, payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(UpdatedUrl)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_WhenSpontaneousUrlIsNotNull_ShouldReturnValidationProblem(string url)
    {
        var jobApplication = await CreateSpontaneousApplicationAsync();
        var payload = GenerateSpontaneousPayload(jobApplication);
        payload["url"] = url;

        var response = await PutAsync(jobApplication.Id, payload);

        await AssertNotUpdatedAsync(response, jobApplication, "url");
    }

    [Fact]
    public async Task Update_WhenSeveralPropertiesAreInvalid_ShouldReturnAllErrors()
    {
        var jobApplication = await CreateOfferApplicationAsync();
        var payload = GenerateOfferPayload(jobApplication);
        payload["title"] = "";
        payload["company"] = "";
        payload["url"] = "acme";

        var response = await PutAsync(jobApplication.Id, payload);

        await AssertNotUpdatedAsync(response, jobApplication, "title", "company", "url");
    }

    // A raw payload, to send missing or wrongly typed properties
    private static Dictionary<string, object?> GenerateOfferPayload(OfferApplication jobApplication)
    {
        return new Dictionary<string, object?>
        {
            ["title"] = $"{jobApplication.Title} updated",
            ["company"] = $"{jobApplication.Company} updated",
            ["url"] = UpdatedUrl
        };
    }

    private static Dictionary<string, object?> GenerateSpontaneousPayload(SpontaneousApplication jobApplication)
    {
        return new Dictionary<string, object?>
        {
            ["title"] = $"{jobApplication.Title} updated",
            ["company"] = $"{jobApplication.Company} updated",
            ["url"] = null
        };
    }

    private Task<HttpResponseMessage> PutAsync(Guid id, Dictionary<string, object?> payload)
    {
        return Client.PutAsJsonAsync($"{BaseUrl}/{id}", payload, TestContext.Current.CancellationToken);
    }

    // A rejected update must leave the job application as it was
    private async Task AssertNotUpdatedAsync(HttpResponseMessage response, JobApplication original,
        params string[] expectedKeys)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadContentAsync<HttpValidationProblemDetails>(response);
        Assert.Equivalent(expectedKeys, problem.Errors.Keys);

        var persisted = await GetPersistedJobApplicationAsync(original.Id);
        Assert.Equal(original.Title, persisted.Title);
        Assert.Equal(original.Company, persisted.Company);

        if (original is OfferApplication offer)
        {
            Assert.Equal(offer.Url, Assert.IsType<OfferApplication>(persisted).Url);
        }
    }
}
