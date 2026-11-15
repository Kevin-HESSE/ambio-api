using System.Globalization;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace Ambio.Api.Abstractions;

public static class PaginationMetaFactory
{
    public static PaginationMeta Create(HttpRequest request, int currentPage, int lastPage, int itemsPerPage,
        int totalItems) => new PaginationMeta(
        ItemsPerPage: itemsPerPage,
        CurrentPage: currentPage,
        TotalItems: totalItems,
        First: BuildLink(request, 1),
        Last: BuildLink(request, lastPage),
        Previous: currentPage > 1 ? BuildLink(request, currentPage - 1) : null,
        Next: currentPage < lastPage ? BuildLink(request, currentPage + 1) : null
    );

    private static string BuildLink(HttpRequest request, int page)
    {
        // ParseQuery is case-insensitive, so "Page" or "page" are both replaced
        var query = QueryHelpers.ParseQuery(request.QueryString.Value);
        query["page"] = page.ToString(CultureInfo.InvariantCulture);

        return UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase,
            request.Path,
            new QueryBuilder(query).ToQueryString());
    }
}
