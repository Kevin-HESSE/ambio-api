using Ambio.Domain.JobApplications;

namespace Ambio.Application.JobApplications;

public static class ApplicationStatusExtensions
{
    public static string ToLabel(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.Draft => "Draft",
        ApplicationStatus.Applied => "Applied",
        ApplicationStatus.Interview => "Interview",
        ApplicationStatus.Offer => "Offer received",
        ApplicationStatus.Rejected => "Rejected",
        ApplicationStatus.Withdrawn => "Withdrawn",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
