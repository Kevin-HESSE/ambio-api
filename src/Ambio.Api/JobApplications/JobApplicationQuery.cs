using System.ComponentModel;

using Ambio.Application.JobApplications;
using Ambio.Domain.JobApplications;

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
    /// Filter by status
    /// </summary>
    public ApplicationStatus? Status { get; init; }

    [property: DefaultValue(OrderOption.Desc), FromQuery(Name = "order[date]")]
    public OrderOption? OrderByDate { get; init; }

    [property: FromQuery(Name = "order[name]")]
    public OrderOption? OrderByName { get; init; }

    /// <summary>
    /// Filter by archived status. Possible values: "none", "only" and "include"
    /// </summary>
    [property: DefaultValue(ArchivedOption.None), FromQuery(Name = "archived")]
    public ArchivedOption? Archived { get; init; } = ArchivedOption.None;
};
