using Ambio.Application.Filters;
using Ambio.Domain.JobApplications;

namespace Ambio.Application.JobApplications;

public interface IJobApplicationFilter : IPaginationFilter
{
    public string? Search { get; init; }
    public string? Company { get; init; }
    public ArchivedOption? Archived { get; init; }
    public ApplicationStatus? Status { get; init; }
    public OrderOption? OrderByDate { get; init; }
    public OrderOption? OrderByName { get; init; }
}
