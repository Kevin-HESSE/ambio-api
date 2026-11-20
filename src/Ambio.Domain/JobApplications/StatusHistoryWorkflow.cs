namespace Ambio.Domain.JobApplications;

public static class StatusHistoryWorkflow
{
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> Transitions = new()
    {
        [ApplicationStatus.Sent] = [ApplicationStatus.Interview, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Interview] = [ApplicationStatus.Offer, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Offer] = [ApplicationStatus.Taken, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Taken] = [],
        [ApplicationStatus.Rejected] = [],
        [ApplicationStatus.Withdrawn] = [],
    };

    public static bool Allows(ApplicationStatus from, ApplicationStatus to) => Transitions[from].Contains(to);
}
