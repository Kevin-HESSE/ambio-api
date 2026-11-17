using Ambio.Application.Filters;

namespace Ambio.Application.JobApplications;

public interface IJobApplicationFilter : IPaginationFilter
{
    public string? Search { get; init; }
    public string? Company { get; init; }
    // Enum filters are read as strings to parse them case-insensitively (see EnumFilter)
    public string? Archived { get; init; }
    public string[]? Status { get; init; }
    public string? OrderByDate { get; init; }
    public string? OrderByTitle { get; init; }
}
