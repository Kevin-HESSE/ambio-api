using System.Net;
using System.Text.Json;

using Ambio.Api.Abstractions;
using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class GetAllTests : BaseJobApplicationTests
{
    // Without arguments, SetUpFixturesAsync registers 100 non-archived and 60 archived job applications
    private const int NonArchivedCount = 100;
    private const int ArchivedCount = 60;
    private const int DefaultItemsPerPage = 10;
    private const int DefaultLastPage = NonArchivedCount / DefaultItemsPerPage;

    public GetAllTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_WhenNoJobApplicationsExist_ShouldReturnEmpty()
    {
        var response = await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<PaginationResponse<JobApplicationResponse>>(response);
        Assert.Empty(content.Data);

        var meta = content.Meta;
        Assert.Equal(0, meta.TotalItems);
        Assert.Equal(1, meta.CurrentPage);
        Assert.Equal(DefaultItemsPerPage, meta.ItemsPerPage);
        AssertLink(meta.First, expectedPage: 1);
        AssertLink(meta.Last, expectedPage: 1);
        Assert.Null(meta.Previous);
        Assert.Null(meta.Next);
    }

    [Fact]
    public async Task GetAll_WhenNoFilterApplied_ShouldReturnTheTenFirstItems()
    {
        await SetUpFixturesAsync();

        var response = await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<PaginationResponse<JobApplicationResponse>>(response);

        Assert.Equal(DefaultItemsPerPage, content.Data.Count());
        Assert.All(content.Data, x => Assert.Null(x.ArchivedAt));

        var meta = content.Meta;
        Assert.Equal(NonArchivedCount, meta.TotalItems);
        Assert.Equal(1, meta.CurrentPage);
        Assert.Equal(DefaultItemsPerPage, meta.ItemsPerPage);
        AssertLink(meta.First, expectedPage: 1);
        AssertLink(meta.Last, expectedPage: DefaultLastPage);
        Assert.Null(meta.Previous);
        AssertLink(meta.Next, expectedPage: 2);
    }

    [Theory]
    [InlineData(1, null, 2)]
    [InlineData(5, 4, 6)]
    [InlineData(DefaultLastPage, DefaultLastPage - 1, null)]
    public async Task GetAll_WhenPageRequested_ShouldReturnMatchingPreviousAndNextLinks(
        int page, int? expectedPrevious, int? expectedNext)
    {
        await SetUpFixturesAsync();

        var response = await Client.GetAsync($"{BaseUrl}?page={page}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<PaginationResponse<JobApplicationResponse>>(response);
        var meta = content.Meta;

        Assert.Equal(page, meta.CurrentPage);
        AssertLink(meta.First, expectedPage: 1);
        AssertLink(meta.Last, expectedPage: DefaultLastPage);
        AssertOptionalLink(meta.Previous, expectedPrevious);
        AssertOptionalLink(meta.Next, expectedNext);
    }

    [Fact]
    public async Task GetAll_WhenPageExceedsLastPage_ShouldReturnLastPage()
    {
        await SetUpFixturesAsync();

        var response = await Client.GetAsync($"{BaseUrl}?page=999", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<PaginationResponse<JobApplicationResponse>>(response);
        Assert.Equal(DefaultItemsPerPage, content.Data.Count());

        var meta = content.Meta;
        Assert.Equal(DefaultLastPage, meta.CurrentPage);
        AssertLink(meta.Last, expectedPage: DefaultLastPage);
        AssertLink(meta.Previous, expectedPage: DefaultLastPage - 1);
        Assert.Null(meta.Next);
    }

    [Fact]
    public async Task GetAll_WhenItemsPerPageProvided_ShouldComputeLastPageAndKeepItInLinks()
    {
        await SetUpFixturesAsync();

        // 100 items, 30 per page: 4 pages, the last one holds 10 items
        var response = await Client.GetAsync($"{BaseUrl}?itemsPerPage=30&page=4",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<PaginationResponse<JobApplicationResponse>>(response);
        Assert.Equal(10, content.Data.Count());

        var meta = content.Meta;
        Assert.Equal(30, meta.ItemsPerPage);
        Assert.Equal(4, meta.CurrentPage);
        AssertLink(meta.First, expectedPage: 1, ("itemsPerPage", "30"));
        AssertLink(meta.Last, expectedPage: 4, ("itemsPerPage", "30"));
        AssertLink(meta.Previous, expectedPage: 3, ("itemsPerPage", "30"));
        Assert.Null(meta.Next);
    }

    [Fact]
    public async Task GetAll_WhenFiltersApplied_ShouldKeepThemInLinks()
    {
        await SetUpFixturesAsync();

        const string company = "Ambio Pagination Company";
        await RegisterJobApplicationsInDatabaseAsync(
            [..FixturesFor.JobApplication.GetOffer(count: 12, company: company)]);

        var url = $"{BaseUrl}?company={Uri.EscapeDataString(company)}&order[date]=Asc&itemsPerPage=5&page=2";
        var response = await Client.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<PaginationResponse<JobApplicationResponse>>(response);
        Assert.Equal(5, content.Data.Count());
        Assert.All(content.Data, x => Assert.Equal(company, x.Company));

        var meta = content.Meta;
        Assert.Equal(12, meta.TotalItems);
        Assert.Equal(2, meta.CurrentPage);

        (string, string)[] expectedFilters =
        [
            ("company", company),
            ("order[date]", "Asc"),
            ("itemsPerPage", "5")
        ];
        AssertLink(meta.First, expectedPage: 1, expectedFilters);
        AssertLink(meta.Last, expectedPage: 3, expectedFilters);
        AssertLink(meta.Previous, expectedPage: 1, expectedFilters);
        AssertLink(meta.Next, expectedPage: 3, expectedFilters);
    }

    [Theory]
    [InlineData(null, NonArchivedCount)]
    [InlineData("none", NonArchivedCount)]
    [InlineData("only", ArchivedCount)]
    [InlineData("included", NonArchivedCount + ArchivedCount)]
    public async Task GetAll_WhenArchivedProvided_ShouldReturnMatchingApplications(string? archived, int expectedTotal)
    {
        await SetUpFixturesAsync();

        var url = archived is null ? BaseUrl : $"{BaseUrl}?archived={archived}&itemsPerPage=100";
        var response = await Client.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<PaginationResponse<JobApplicationResponse>>(response);
        Assert.Equal(expectedTotal, content.Meta.TotalItems);

        switch (archived)
        {
            case null or "none":
                Assert.All(content.Data, x => Assert.Null(x.ArchivedAt));
                break;
            case "only":
                Assert.All(content.Data, x => Assert.NotNull(x.ArchivedAt));
                break;
            default:
                Assert.Contains(content.Data, x => x.ArchivedAt is null);
                Assert.Contains(content.Data, x => x.ArchivedAt is not null);
                break;
        }
    }

    [Fact]
    public async Task GetAll_WhenSearchProvided_ShouldMatchTitlePartiallyIgnoringCase()
    {
        await SetUpFixturesAsync(
            FixturesFor.JobApplication.GetOffer(title: "Senior PHP Developer"),
            FixturesFor.JobApplication.GetOffer(title: "php engineer"),
            FixturesFor.JobApplication.GetOffer(title: "Java Developer"));

        var content = await GetCollectionAsync("?search=PhP");

        Assert.Equal(2, content.Meta.TotalItems);
        Assert.Equivalent(new[] { "Senior PHP Developer", "php engineer" }, content.Data.Select(x => x.Title));
    }

    [Fact]
    public async Task GetAll_WhenSearchContainsWildcard_ShouldMatchItLiterally()
    {
        await SetUpFixturesAsync(
            FixturesFor.JobApplication.GetOffer(title: "100% remote"),
            FixturesFor.JobApplication.GetOffer(title: "Backend developer"));

        var content = await GetCollectionAsync($"?search={Uri.EscapeDataString("%")}");

        var single = Assert.Single(content.Data);
        Assert.Equal("100% remote", single.Title);
    }

    [Fact]
    public async Task GetAll_WhenCompanyProvided_ShouldMatchExactlyIgnoringCase()
    {
        await SetUpFixturesAsync(
            FixturesFor.JobApplication.GetOffer(company: "Acme"),
            FixturesFor.JobApplication.GetOffer(company: "ACME"),
            FixturesFor.JobApplication.GetOffer(company: "Acme Corp"));

        var content = await GetCollectionAsync("?company=acme");

        Assert.Equal(2, content.Meta.TotalItems);
        Assert.Equivalent(new[] { "Acme", "ACME" }, content.Data.Select(x => x.Company));
    }

    [Fact]
    public async Task GetAll_WhenTextFiltersAreEmpty_ShouldIgnoreThem()
    {
        await SetUpFixturesAsync();

        var content = await GetCollectionAsync("?search=&company=%20");

        Assert.Equal(NonArchivedCount, content.Meta.TotalItems);
    }

    [Fact]
    public async Task GetAll_WhenEnumFiltersAreEmpty_ShouldIgnoreThem()
    {
        await SetUpFixturesAsync();

        var content = await GetCollectionAsync("?status=&archived=&order[date]=&order[title]=%20");

        Assert.Equal(NonArchivedCount, content.Meta.TotalItems);
        Assert.All(content.Data, x => Assert.Null(x.ArchivedAt));
    }

    [Fact]
    public async Task GetAll_WhenStatusProvided_ShouldReturnOnlyMatchingStatus()
    {
        await RegisterOneApplicationPerStatusAsync();

        var content = await GetCollectionAsync("?status=interview");

        var single = Assert.Single(content.Data);
        Assert.Equal(ApplicationStatus.Interview, single.CurrentStatus);
    }

    [Fact]
    public async Task GetAll_WhenStatusRepeated_ShouldReturnAllMatchingStatuses()
    {
        await RegisterOneApplicationPerStatusAsync();

        var content = await GetCollectionAsync("?status=sent&status=interview");

        Assert.Equal(2, content.Meta.TotalItems);
        Assert.Equivalent(
            new[] { ApplicationStatus.Sent, ApplicationStatus.Interview },
            content.Data.Select(x => x.CurrentStatus));
    }

    [Fact]
    public async Task GetAll_WhenStatusRepeated_ShouldKeepAllValuesInLinks()
    {
        await SetUpFixturesAsync(
        [
            ..FixturesFor.JobApplication.GetOffer(count: 6, status: ApplicationStatus.Sent),
            ..FixturesFor.JobApplication.GetOffer(count: 6, status: ApplicationStatus.Interview),
            ..FixturesFor.JobApplication.GetOffer(count: 3, status: ApplicationStatus.Offer)
        ]);

        var content = await GetCollectionAsync("?status=sent&status=interview&itemsPerPage=5&page=2");

        var meta = content.Meta;
        Assert.Equal(12, meta.TotalItems);
        AssertLinkValues(meta.First, "status", "sent", "interview");
        AssertLinkValues(meta.Previous, "status", "sent", "interview");
        AssertLinkValues(meta.Next, "status", "sent", "interview");
        AssertLinkValues(meta.Last, "status", "sent", "interview");
    }

    [Fact]
    public async Task GetAll_WhenFiltersCombined_ShouldApplyAllOfThem()
    {
        await SetUpFixturesAsync(
            FixturesFor.JobApplication.GetOffer(title: "Expected", company: "Acme", status: ApplicationStatus.Offer,
                archived: true),
            FixturesFor.JobApplication.GetOffer(company: "Acme", status: ApplicationStatus.Offer),
            FixturesFor.JobApplication.GetOffer(company: "Acme", status: ApplicationStatus.Sent, archived: true),
            FixturesFor.JobApplication.GetOffer(company: "Globex", status: ApplicationStatus.Offer, archived: true));

        var content = await GetCollectionAsync("?company=acme&status=offer&archived=only");

        var single = Assert.Single(content.Data);
        Assert.Equal("Expected", single.Title);
    }

    [Fact]
    public async Task GetAll_WhenNoOrderProvided_ShouldSortByMostRecentFirst()
    {
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(title: "A"));
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(title: "B"));
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(title: "C"));

        var content = await GetCollectionAsync(string.Empty);

        Assert.Equal(["C", "B", "A"], content.Data.Select(x => x.Title));
    }

    [Fact]
    public async Task GetAll_WhenOrderByDateAsc_ShouldSortByOldestFirst()
    {
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(title: "A"));
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(title: "B"));
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(title: "C"));

        var content = await GetCollectionAsync("?order[date]=asc");

        Assert.Equal(["A", "B", "C"], content.Data.Select(x => x.Title));
    }

    [Theory]
    [InlineData("asc", new[] { "A", "B", "C" })]
    [InlineData("desc", new[] { "C", "B", "A" })]
    public async Task GetAll_WhenOrderByTitleProvided_ShouldSortByTitle(string order, string[] expectedTitles)
    {
        // Same batch: same creation date, so only the title decides
        await SetUpFixturesAsync(
            FixturesFor.JobApplication.GetOffer(title: "B"),
            FixturesFor.JobApplication.GetOffer(title: "A"),
            FixturesFor.JobApplication.GetOffer(title: "C"));

        var content = await GetCollectionAsync($"?order[title]={order}");

        Assert.Equal(expectedTitles, content.Data.Select(x => x.Title));
    }

    [Fact]
    public async Task GetAll_WhenBothOrdersProvided_ShouldSortByDateFirst()
    {
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(title: "A"));
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(title: "Z"));

        var content = await GetCollectionAsync("?order[title]=asc");

        Assert.Equal(["Z", "A"], content.Data.Select(x => x.Title));
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("itemsPerPage=0")]
    [InlineData("itemsPerPage=101")]
    [InlineData("archived=foo")]
    [InlineData("status=foo")]
    [InlineData("status=99")]
    [InlineData("status=3")]
    [InlineData("order[date]=foo")]
    [InlineData("order[title]=foo")]
    public async Task GetAll_WhenFilterIsInvalid_ShouldReturnBadRequest(string queryString)
    {
        var response = await Client.GetAsync($"{BaseUrl}?{queryString}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("status=interview,offer", "status[0]")]
    [InlineData("archived=none,only", "archived")]
    [InlineData("order[date]=asc,desc", "order[date]")]
    [InlineData("order[title]=asc,desc", "order[title]")]
    public async Task GetAll_WhenEnumFilterUsesCommas_ShouldReturnValidationProblem(string queryString, string key)
    {
        var response = await Client.GetAsync($"{BaseUrl}?{queryString}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadContentAsync<HttpValidationProblemDetails>(response);
        Assert.Contains(key, problem.Errors.Keys);
    }

    [Fact]
    public async Task GetAll_WhenFilterCannotBeParsed_ShouldReturnProblemDetails()
    {
        // A non-numeric page is rejected by the parameter binding, before FluentValidation
        var response = await Client.GetAsync($"{BaseUrl}?page=abc", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("itemsPerPage=101", "itemsPerPage")]
    [InlineData("status=sent&status=foo", "status[1]")]
    [InlineData("archived=foo", "archived")]
    [InlineData("order[date]=foo", "order[date]")]
    [InlineData("order[title]=foo", "order[title]")]
    public async Task GetAll_WhenValidationFails_ShouldUseQueryParameterNames(string queryString, string expectedKey)
    {
        var response = await Client.GetAsync($"{BaseUrl}?{queryString}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadContentAsync<HttpValidationProblemDetails>(response);
        Assert.Contains(expectedKey, problem.Errors.Keys);
    }

    [Fact]
    public async Task GetAll_ShouldSerializeFieldsAsDefinedInConception()
    {
        await SetUpFixturesAsync(FixturesFor.JobApplication.GetOffer(status: ApplicationStatus.Interview));

        var response = await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var item = document.RootElement.GetProperty("data")[0];

        Assert.Equal("interview", item.GetProperty("currentStatus").GetString());
        Assert.Equal("Offer", item.GetProperty("kind").GetString());
        Assert.True(item.TryGetProperty("appliedOn", out _));
        Assert.True(item.TryGetProperty("archivedAt", out _));
        Assert.True(item.TryGetProperty("url", out _));
    }

    private async Task<PaginationResponse<JobApplicationResponse>> GetCollectionAsync(string queryString)
    {
        var response = await Client.GetAsync($"{BaseUrl}{queryString}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await ReadContentAsync<PaginationResponse<JobApplicationResponse>>(response);
    }

    private async Task RegisterOneApplicationPerStatusAsync()
    {
        await SetUpFixturesAsync(Enum.GetValues<ApplicationStatus>()
            .Select(status => (JobApplication)FixturesFor.JobApplication.GetOffer(status: status))
            .ToArray());
    }

    private static void AssertLinkValues(string? link, string key, params string[] expectedValues)
    {
        Assert.NotNull(link);
        var query = QueryHelpers.ParseQuery(new Uri(link).Query);

        Assert.True(query.ContainsKey(key), $"'{link}' does not keep the '{key}' filter");
        Assert.Equal(expectedValues, query[key].ToArray());
    }

    private static void AssertOptionalLink(string? link, int? expectedPage)
    {
        if (expectedPage is null)
        {
            Assert.Null(link);
            return;
        }

        AssertLink(link, expectedPage.Value);
    }

    private static void AssertLink(string? link, int expectedPage, params (string Key, string Value)[] expectedFilters)
    {
        Assert.NotNull(link);
        Assert.True(Uri.TryCreate(link, UriKind.Absolute, out var uri), $"'{link}' is not an absolute URL");
        Assert.Equal(BaseUrl, uri.AbsolutePath);

        var query = QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal(expectedPage.ToString(), query["page"].ToString());

        foreach (var (key, value) in expectedFilters)
        {
            Assert.True(query.ContainsKey(key), $"'{link}' does not keep the '{key}' filter");
            Assert.Equal(value, query[key].ToString());
        }
    }

    // TimestampInterceptor stamps every entity of a SaveChanges with the same CreatedAt,
    // so the clock moves forward after each call to give the next batch a later date
    private async Task SetUpFixturesAsync()
    {
        List<JobApplication> jobApplications =
        [
            ..FixturesFor.JobApplication.GetOffer(count: 50),
            ..FixturesFor.JobApplication.GetOffer(count: 10, archived: true),
            ..FixturesFor.JobApplication.GetSpontaneous(count: 50),
            ..FixturesFor.JobApplication.GetSpontaneous(count: 50, archived: true),
        ];

        await RegisterJobApplicationsInDatabaseAsync(jobApplications);
        FakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
    }

    private async Task SetUpFixturesAsync(params JobApplication[] jobApplications)
    {
        await RegisterJobApplicationsInDatabaseAsync([.. jobApplications]);
        FakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
    }
}
