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

    /// <summary>Statuses exposed by the API (<see cref="ApplicationStatus.Created"/> is internal).</summary>
    public static IReadOnlySet<ApplicationStatus> Statuses { get; } = Transitions.Keys.ToHashSet();

    /// <summary>Statuses an application can move to.</summary>
    public static IReadOnlySet<ApplicationStatus> Targets { get; } = Transitions.Values.SelectMany(t => t).ToHashSet();

    public static bool IsNameOf(string? value, IEnumerable<ApplicationStatus> statuses) =>
        statuses.Any(s => string.Equals(s.ToString(), value, StringComparison.OrdinalIgnoreCase));

    public static bool Allows(ApplicationStatus from, ApplicationStatus to) => Transitions[from].Contains(to);
}
