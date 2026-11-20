using System.Net;
using System.Net.Http.Json;

using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class ChangeStatusTests : BaseJobApplicationTests
{
    public ChangeStatusTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData(ApplicationKind.Offer)]
    [InlineData(ApplicationKind.Spontaneous)]
    public async Task ChangeStatus_WhenTransitionIsAllowed_ShouldReturnNoContent(ApplicationKind kind)
    {
        var jobApplication = await CreateJobApplicationAsync(kind, ApplicationStatus.Sent);

        var response = await ChangeStatusAsync(jobApplication.Id, "interview");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    [Theory]
    [InlineData(ApplicationStatus.Sent, ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Sent, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Sent, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Offer)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Offer, ApplicationStatus.Taken)]
    [InlineData(ApplicationStatus.Offer, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Offer, ApplicationStatus.Withdrawn)]
    public async Task ChangeStatus_WhenTransitionIsAllowed_ShouldPersistNewStatus(
        ApplicationStatus from,
        ApplicationStatus to)
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, from);

        var response = await ChangeStatusAsync(jobApplication.Id, to.ToString().ToLowerInvariant());
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var persisted = await GetPersistedJobApplicationAsync(jobApplication.Id);
        Assert.Equal(to, persisted.Status);
    }

    [Fact]
    public async Task ChangeStatus_WhenStatusCaseDiffers_ShouldReturnNoContent()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);

        var response = await ChangeStatusAsync(jobApplication.Id, "INTERVIEW");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WhenTransitionIsAllowed_ShouldAddHistoryEntry()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);
        var date = jobApplication.AppliedOn.AddDays(1);

        var response = await ChangeStatusAsync(jobApplication.Id, "interview", date);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var history = await GetPersistedHistoryAsync(jobApplication.Id);
        Assert.Equal(2, history.Count);

        var last = history[^1];
        Assert.Equal(ApplicationStatus.Sent, last.StatusFrom);
        Assert.Equal(ApplicationStatus.Interview, last.StatusTo);
        Assert.Equal(date, last.ChangedAt);
    }

    [Fact]
    public async Task ChangeStatus_WhenDateIsNull_ShouldUseToday()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);
        var today = TestClock.TodayFromProvider(FakeTimeProvider);

        var response = await ChangeStatusAsync(jobApplication.Id, "interview");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var history = await GetPersistedHistoryAsync(jobApplication.Id);
        Assert.Equal(today, history[^1].ChangedAt);
    }

    [Fact]
    public async Task ChangeStatus_WhenDateEqualsPreviousStatusDate_ShouldReturnNoContent()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);

        var response = await ChangeStatusAsync(jobApplication.Id, "interview", jobApplication.AppliedOn);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WhenSeveralChangesAreMade_ShouldReturnOrderedHistory()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);

        foreach (var status in new[] { "interview", "offer", "taken" })
        {
            // Entries are ordered by created_at: the clock must move between two changes
            FakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
            var response = await ChangeStatusAsync(jobApplication.Id, status);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        var getResponse = await Client.GetAsync($"{BaseUrl}/{jobApplication.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var content = await ReadContentAsync<JobApplicationDetailedResponse>(getResponse);
        Assert.Equal(ApplicationStatus.Taken, content.CurrentStatus);
        Assert.Equal(
            [
                ApplicationStatus.Sent,
                ApplicationStatus.Interview,
                ApplicationStatus.Offer,
                ApplicationStatus.Taken
            ],
            content.History.Select(h => h.Status));
    }

    [Fact]
    public async Task ChangeStatus_WhenDateIsBeforePreviousStatusDate_ShouldReturnValidationProblem()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);

        var response = await ChangeStatusAsync(jobApplication.Id, "interview", jobApplication.AppliedOn.AddDays(-1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertUnchangedAsync(jobApplication.Id, ApplicationStatus.Sent, expectedHistoryCount: 1);
    }

    [Fact]
    public async Task ChangeStatus_WhenDateIsInTheFuture_ShouldReturnValidationProblem()
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);
        var tomorrow = TestClock.TodayFromProvider(FakeTimeProvider).AddDays(1);

        var response = await ChangeStatusAsync(jobApplication.Id, "interview", tomorrow);

        await AssertStatusValidationProblemAsync(response, "date");
        await AssertUnchangedAsync(jobApplication.Id, ApplicationStatus.Sent, expectedHistoryCount: 1);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("sent,interview")]
    [InlineData(null)]
    public async Task ChangeStatus_WhenStatusIsInvalid_ShouldReturnValidationProblem(string? status)
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, ApplicationStatus.Sent);

        var response = await ChangeStatusAsync(jobApplication.Id, status);

        await AssertStatusValidationProblemAsync(response, "status");
        await AssertUnchangedAsync(jobApplication.Id, ApplicationStatus.Sent, expectedHistoryCount: 1);
    }

    [Theory]
    [InlineData(ApplicationStatus.Sent)]
    [InlineData(ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Offer)]
    public async Task ChangeStatus_WhenStatusIsIdentical_ShouldReturnConflict(ApplicationStatus status)
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, status);
        var historyCount = PathTo[status].Length + 1;

        var response = await ChangeStatusAsync(jobApplication.Id, status.ToString().ToLowerInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertUnchangedAsync(jobApplication.Id, status, historyCount);
    }

    [Theory]
    [InlineData(ApplicationStatus.Sent, ApplicationStatus.Offer)]
    [InlineData(ApplicationStatus.Sent, ApplicationStatus.Taken)]
    [InlineData(ApplicationStatus.Sent, ApplicationStatus.Created)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Sent)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Taken)]
    [InlineData(ApplicationStatus.Offer, ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Taken, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Withdrawn, ApplicationStatus.Offer)]
    public async Task ChangeStatus_WhenTransitionIsNotAllowed_ShouldReturnConflict(
        ApplicationStatus from,
        ApplicationStatus to)
    {
        var jobApplication = await CreateJobApplicationAsync(ApplicationKind.Offer, from);
        var historyCount = PathTo[from].Length + 1;

        var response = await ChangeStatusAsync(jobApplication.Id, to.ToString().ToLowerInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertUnchangedAsync(jobApplication.Id, from, historyCount);
    }

    [Theory]
    [InlineData(ApplicationKind.Offer)]
    [InlineData(ApplicationKind.Spontaneous)]
    public async Task ChangeStatus_WhenJobApplicationIsArchived_ShouldReturnConflict(ApplicationKind kind)
    {
        var jobApplication = await CreateJobApplicationAsync(kind, ApplicationStatus.Sent, archived: true);

        var response = await ChangeStatusAsync(jobApplication.Id, "interview");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertUnchangedAsync(jobApplication.Id, ApplicationStatus.Sent, expectedHistoryCount: 1);
    }

    [Fact]
    public async Task ChangeStatus_WhenJobApplicationDoesNotExist_ShouldReturnNotFound()
    {
        var response = await ChangeStatusAsync(Guid.CreateVersion7(), "interview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> ChangeStatusAsync(Guid id, string? status, DateOnly? date = null)
    {
        return Client.PostAsJsonAsync(
            $"{BaseUrl}/{id}/status",
            new { status, date },
            TestContext.Current.CancellationToken);
    }

    private async Task AssertUnchangedAsync(Guid id, ApplicationStatus expectedStatus, int expectedHistoryCount)
    {
        var persisted = await GetPersistedJobApplicationAsync(id);
        Assert.Equal(expectedStatus, persisted.Status);

        var history = await GetPersistedHistoryAsync(id);
        Assert.Equal(expectedHistoryCount, history.Count);
    }

    // Unlike AssertValidationProblemAsync, the application already exists in the database
    private static async Task AssertStatusValidationProblemAsync(HttpResponseMessage response, string expectedKey)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadContentAsync<Microsoft.AspNetCore.Http.HttpValidationProblemDetails>(response);
        Assert.Contains(expectedKey, problem.Errors.Keys);
    }
}
