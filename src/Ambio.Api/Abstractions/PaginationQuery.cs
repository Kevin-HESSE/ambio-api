using System.ComponentModel;

using Microsoft.AspNetCore.Mvc;

namespace Ambio.Api.Abstractions;

public record PaginationQuery(
    [property: DefaultValue(1), FromQuery(Name = "page")] int Page = 1,
    [property: DefaultValue(10), FromQuery(Name = "itemsPerPage")] int ItemsPerPage = 10);
