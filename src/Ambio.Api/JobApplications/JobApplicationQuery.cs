using System.ComponentModel;

using Ambio.Application.JobApplications;

using Microsoft.AspNetCore.Mvc;

namespace Ambio.Api.JobApplications;

public sealed record JobApplicationQuery : IJobApplicationFilter
{
    [property: DefaultValue(1)]
    public int? Page { get; init; } = 1;

    [property: DefaultValue(10)]
    public int? ItemsPerPage { get; init; } = 10;

    /// <summary>
    /// Filter by search term
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Filter by company name
    /// </summary>
    public string? Company { get; init; }

    /// <summary>
    /// Filter by status, repeat the parameter for several values. Possible values: "sent", "interview", "offer",
    /// "taken", "rejected" and "withdrawn"
    /// </summary>
    [property: FromQuery(Name = "status")]
    public string[]? Status { get; init; }

    /// <summary>
    /// Sort by creation date. Possible values: "asc" and "desc"
    /// </summary>
    [property: DefaultValue("desc"), FromQuery(Name = "order[date]")]
    public string? OrderByDate { get; init; }

    /// <summary>
    /// Sort by title. Possible values: "asc" and "desc"
    /// </summary>
    [property: FromQuery(Name = "order[title]")]
    public string? OrderByTitle { get; init; }

    /// <summary>
    /// Filter by archived status. Possible values: "none", "only" and "included"
    /// </summary>
    [property: DefaultValue("none"), FromQuery(Name = "archived")]
    public string? Archived { get; init; }
};
