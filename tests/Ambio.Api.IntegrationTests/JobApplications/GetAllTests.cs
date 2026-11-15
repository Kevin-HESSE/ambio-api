using System.Net;

using Ambio.Api.Abstractions;
using Ambio.Api.IntegrationTests.Fixtures;
using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

using Microsoft.AspNetCore.WebUtilities;

namespace Ambio.Api.IntegrationTests.JobApplications;

public class GetAllTests : BaseJobApplicationTests
{
    // SetUpFixturesAsync registers 100 non-archived job applications
    private const int NonArchivedCount = 100;
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
        var companyApplications = FixturesFor.JobApplication.GetOffer(count: 12)
            .Select(ja =>
            {
                ja.Company = company;
                return (JobApplication)ja;
            })
            .ToList();
        await RegisterJobApplicationsInDatabaseAsync(companyApplications);

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
    }
}
