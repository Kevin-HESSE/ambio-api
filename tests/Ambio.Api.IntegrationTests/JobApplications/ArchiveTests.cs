using System.Net;

using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Domain.JobApplications;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class ArchiveTests : BaseJobApplicationTests
{
    public ArchiveTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData(ApplicationKind.Offer)]
    [InlineData(ApplicationKind.Spontaneous)]
    public async Task Archive_WhenJobApplicationExists_ShouldReturnNoContent(ApplicationKind kind)
    {
        var jobApplication = await CreateJobApplicationAsync(kind);

        var response = await ArchiveAsync(jobApplication.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    [Theory]
    [InlineData(ApplicationKind.Offer)]
    [InlineData(ApplicationKind.Spontaneous)]
    public async Task Archive_WhenJobApplicationExists_ShouldPersistArchiveDate(ApplicationKind kind)
    {
        var jobApplication = await CreateJobApplicationAsync(kind);
        FakeTimeProvider.Advance(TimeSpan.FromDays(1));
        var expectedArchivedAt = FakeTimeProvider.GetUtcNow().UtcDateTime;

        var response = await ArchiveAsync(jobApplication.Id);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var persisted = await GetPersistedJobApplicationAsync(jobApplication.Id);
        Assert.Equal(expectedArchivedAt, persisted.ArchivedAt);
    }

    [Fact]
    public async Task Archive_WhenJobApplicationExists_ShouldKeepItInDatabase()
    {
        var jobApplication = await CreateOfferApplicationAsync();

        var response = await ArchiveAsync(jobApplication.Id);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await AssertNumberOfJobApplicationsInDatabaseAsync(1);
    }

    [Fact]
    public async Task Archive_WhenJobApplicationDoesNotExist_ShouldReturnNotFound()
    {
        var response = await ArchiveAsync(Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Archive_WhenJobApplicationIsAlreadyArchived_ShouldReturnConflict()
    {
        var jobApplication = await CreateOfferApplicationAsync(archived: true);

        var response = await ArchiveAsync(jobApplication.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Archive_WhenJobApplicationIsAlreadyArchived_ShouldKeepTheOriginalArchiveDate()
    {
        var jobApplication = await CreateOfferApplicationAsync(archived: true);
        // Read back from the database: PostgreSQL truncates the generated date to the microsecond
        var originalArchivedAt = (await GetPersistedJobApplicationAsync(jobApplication.Id)).ArchivedAt;
        FakeTimeProvider.Advance(TimeSpan.FromDays(1));

        var response = await ArchiveAsync(jobApplication.Id);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var persisted = await GetPersistedJobApplicationAsync(jobApplication.Id);
        Assert.Equal(originalArchivedAt, persisted.ArchivedAt);
    }

    private Task<HttpResponseMessage> ArchiveAsync(Guid id)
    {
        return Client.PostAsync($"{BaseUrl}/{id}/archive", content: null, TestContext.Current.CancellationToken);
    }
}
